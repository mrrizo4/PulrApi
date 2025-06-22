using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Categories.Queries;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Products.Queries
{
    public class GetProductDetailsQuery : IRequest<ProductDetailsResponse>
    {
        [Required] public string Uid { get; set; }
        public string CurrencyCode { get; set; }
        public string AffiliateId { get; set; }
    }

    public class GetProductDetailsQueryHandler : IRequestHandler<GetProductDetailsQuery, ProductDetailsResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetProductDetailsQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly IStoreService _storeService;

        public GetProductDetailsQueryHandler(
            IApplicationDbContext dbContext,
            ILogger<GetProductDetailsQueryHandler> logger,
            ICurrentUserService currentUserService,
            IExchangeRateService exchangeRateService,
            IStoreService storeService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUserService = currentUserService;
            _exchangeRateService = exchangeRateService;
            _storeService = storeService;
        }

        public async Task<ProductDetailsResponse> Handle(GetProductDetailsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                var affiliateId = request.AffiliateId;

                if (affiliateId != null)
                {
                    affiliateId = await _dbContext.Affiliates.Where(a => a.AffiliateId == affiliateId)
                        .Select(a => a.AffiliateId).FirstOrDefaultAsync(cancellationToken);
                }

                var product = await _dbContext.Products.Where(p => p.IsActive && p.Uid == request.Uid)
                    .Select(p =>
                        new ProductDetailsResponse
                        {
                            Uid = p.Uid,
                            StoreUid = p.Store.Uid,
                            StoreUniqueName = p.Store.UniqueName,
                            Name = p.Name,
                            Categories = p.ProductCategory.Select(pc => new SingleCategoryResponse
                            {
                                Uid = pc.Category.Uid,
                                Name = pc.Category.Name
                            }).ToList(),
                            Price = p.Price,
                            CurrencyCode = p.Store.Currency.Code,
                            Quantity = p.Quantity,
                            ArticleCode = p.ArticleCode,
                            AffiliateId = affiliateId,
                            ProductAttributes = p.ProductAttributes.Select(pv => new ProductAttributeResponse()
                            {
                                Uid = pv.Uid,
                                Key = pv.Key,
                                Values = String.Join(",", pv.ProductAttributeValues.Select(pav => pav.Value))
                            }),
                            Description = p.Description,
                            MoreInfos = p.ProductMoreInfos.Select(pmi => new ProductMoreInfoResponse()
                                { Uid = pmi.Uid, Info = pmi.Info, Title = pmi.Title }).ToList(),
                            ProductMediaFiles = p.ProductMediaFiles.Select(pmf =>
                                new MediaFileDetailsResponse()
                                {
                                    Uid = pmf.MediaFile.Uid,
                                    FileType = pmf.MediaFile.MediaFileType.ToString(),
                                    Url = pmf.MediaFile.Url,
                                    Priority = pmf.MediaFile.Priority
                                }).ToList(),
                            ProductPairArticleCodes = p.ProductPairs.Select(pp => pp.Pair.ArticleCode),
                            ProductSimilarArticleCodes = p.ProductSimilars.Select(ps => ps.Similar.ArticleCode),
                            LikesCount = p.ProductLikes.Count,
                            LikedByMe = cUser != null
                                ? p.ProductLikes.Any(pl => pl.LikedById == cUser.Profile.Id)
                                : false,
                        })
                    .AsNoTracking()
                    .SingleOrDefaultAsync(cancellationToken);

                if (product == null)
                {
                    throw new BadRequestException("Product doesn't exist.");
                }

                product.TaggedBy = await _dbContext.PostProductTags.Where(ppt => ppt.Product.Uid == product.Uid)
                    .OrderByDescending(ppt => ppt.CreatedAt)
                    .Select(ppt => new TaggedByDto()
                    {
                        Uid = ppt.Post.User.Profile.Uid,
                        Username = ppt.Post.User.UserName,
                        FullName = ppt.Post.User.FirstName,
                        FirstName = ppt.Post.User.FirstName,
                        LastName = ppt.Post.User.LastName,
                        ImageUrl = ppt.Post.User.Profile.ImageUrl,
                        PostedTimeAgo = ppt.CreatedAt,
                        PostLikes = ppt.Post.PostLikes.Count(),
                        PostMediaFile = new MediaFileDetailsResponse()
                        {
                            FileType = ppt.Post.MediaFile.MediaFileType.ToString(),
                            Uid = ppt.Post.MediaFile.Uid,
                            Url = ppt.Post.MediaFile.Url
                        }
                    }).Take(4)
                    .ToListAsync(cancellationToken);

                if (String.IsNullOrWhiteSpace(request.CurrencyCode))
                {
                    return product;
                }

                var exchangeRates = await _exchangeRateService
                    .GetExchangeRates(new List<string> { request.CurrencyCode, product.CurrencyCode });

                product.Price = _exchangeRateService.GetCurrencyExchangeRates(
                    product.CurrencyCode,
                    request.CurrencyCode,
                    product.Price,
                    exchangeRates);
                product.CurrencyCode = request.CurrencyCode;

                return product;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
