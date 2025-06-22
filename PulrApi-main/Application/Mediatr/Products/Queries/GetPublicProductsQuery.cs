using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Products.Queries;
using Core.Application.Models;
using Core.Application.Models.Products;
using Core.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace Core.Application.Mediatr.Products.Queries
{
    public class GetPublicProductsQuery : PagingParamsRequest, IRequest<PagingResponse<ProductPublicResponse>>
    {
        public string StoreUid { get; set; }
        public string CategoryUid { get; set; }
        public string PostUid { get; set; }
        public string CurrencyCode { get; set; }
    }

    public class GetPublicProductsQueryHandler : IRequestHandler<GetPublicProductsQuery, PagingResponse<ProductPublicResponse>>
    {
        private readonly ILogger<GetPublicProductsQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly IQueryHelperService _queryHelperService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public GetPublicProductsQueryHandler(
            ILogger<GetPublicProductsQueryHandler> logger,
            IApplicationDbContext dbContext,
            IQueryHelperService queryHelperService,
            IExchangeRateService exchangeRateService,
            IConfiguration configuration,
            IMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _queryHelperService = queryHelperService;
            _exchangeRateService = exchangeRateService;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<PagingResponse<ProductPublicResponse>> Handle(GetPublicProductsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<Product> query = _dbContext.Products;
                query = query.Where(e => e.IsActive == true);

                if (!String.IsNullOrWhiteSpace(request.StoreUid))
                {
                    query = query.Where(e => e.Store.Uid == request.StoreUid);
                }

                //TODO rewrite this filter
                /*if (!String.IsNullOrWhiteSpace(request.CategoryUid))
                {
                    query = query.Where(p => p.ProductCategory.Uid == request.CategoryUid);
                }*/

                string affiliateId = null;
                if (!String.IsNullOrWhiteSpace(request.PostUid))
                {
                    var postProducts = await _dbContext.PostProductTags
                        .Where(ppt => ppt.Post.Uid == request.PostUid)
                        .Include(p => p.Product).ThenInclude(p => p.Store)
                        .ToListAsync(cancellationToken);
                    
                    query = query.Where(p => postProducts.Select(ppt => ppt.Product.Uid).Contains(p.Uid));

                    affiliateId = await _dbContext.Users
                        .Where(u => u.Posts.Select(p => p.Uid).Contains(request.PostUid))
                        .Select(u => u.Affiliate.AffiliateId).FirstOrDefaultAsync(cancellationToken);
                }

                if (!String.IsNullOrWhiteSpace(request.Search))
                {
                    query = query.Where(p => p.Name.ToLower().Contains(request.Search.Trim().ToLower()) ||
                                             p.Description.ToLower().Contains(request.Search.Trim().ToLower()));
                }

                if (String.IsNullOrWhiteSpace(request.Order) || String.IsNullOrWhiteSpace(request.OrderBy))
                {
                    query = query.OrderByDescending(u => u.Id);
                }
                else
                {
                    query = _queryHelperService.AppendOrderBy(query, request.OrderBy, request.Order);
                }

                Currency fallbackCurrencyCode = null;
                Currency storeCurrencyCode = await _dbContext.Stores.Where(s => s.Uid == request.StoreUid)
                    .Select(s => s.Currency).SingleOrDefaultAsync(cancellationToken);
                List<ExchangeRate> exchangeRates = null;

                if (!String.IsNullOrEmpty(request.CurrencyCode))
                {
                    exchangeRates = await _exchangeRateService.GetExchangeRates(  new List<string> { request.CurrencyCode });
                }


                query = query.Select(p => new Product()
                {
                    Id = p.Id,
                    Uid = p.Uid,
                    Name = p.Name,
                    Store = new Store()
                    {
                        Uid = p.Store.Uid,
                        Name = p.Store.Name,
                        Currency = p.Store.Currency
                    },
                    Price = request.CurrencyCode != null
                        ? _exchangeRateService.GetCurrencyExchangeRates(storeCurrencyCode != null ? storeCurrencyCode.Code : _configuration["ProfileSettings:DefaultCurrencyCode"], request.CurrencyCode,
                            p.Price, exchangeRates)
                        : p.Price
                }).AsNoTracking();

                //var queryRaw = query.ToSql();

                var list = await PagedList<Product>.ToPagedListAsync(query, request.PageNumber, request.PageSize);
                var listOfProductUids = list.Select(p => p.Uid);

                var productMediaFileList = await _dbContext.ProductMediaFiles.Where(pmf =>
                        listOfProductUids.Contains(pmf.Product.Uid) &&
                        pmf.MediaFile.Priority == 0)
                    .Select(pmf => new ProductMediaFileDto()
                    {
                        MediaFileUrl = pmf.MediaFile.Url,
                        ProductUid = pmf.Product.Uid
                    })
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);


                if (list.Count > 0 && String.IsNullOrEmpty(request.StoreUid) || String.IsNullOrWhiteSpace(request.CurrencyCode))
                {
                    fallbackCurrencyCode = list.Select(p => p.Store.Currency).FirstOrDefault();
                }

                var mappedList = _mapper.Map<PagingResponse<ProductPublicResponse>>(list);

                for (int i = 0; i < mappedList.Items.Count; i++)
                {
                    var item = mappedList.Items[i];
                    item.AffiliateId = affiliateId;
                    item.CurrencyUid = storeCurrencyCode?.Uid ?? fallbackCurrencyCode.Uid;
                    item.CurrencyCode = storeCurrencyCode?.Code ?? fallbackCurrencyCode.Code;
                    item.FeaturedImageUrl = productMediaFileList.Where(pmfl => pmfl.ProductUid == item.Uid)
                        .Select(pmf => pmf.MediaFileUrl)
                        .SingleOrDefault();
                }

                mappedList.ItemIds = mappedList.Items.Select(item => item.Uid).ToList();
                return mappedList;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}