using System;
using System.Linq;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Core.Application.Mediatr.BagItems.Queries;
using Core.Application.Models.BagItems;
using Core.Application.Models.Currencies;
using Core.Application.Models.MediaFiles;

namespace Core.Application.Mediatr.BagItems.Queries
{
    public class GetBagItemsQuery : IRequest<BagResponse>
    {
    }

    public class GetBagItemsQueryHandler : IRequestHandler<GetBagItemsQuery, BagResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetBagItemsQueryHandler> _logger;

        public GetBagItemsQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            ILogger<GetBagItemsQueryHandler> logger)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<BagResponse> Handle(GetBagItemsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync(false);
                var myBagResponse = new BagResponse();

                if (cUser == null)
                    return myBagResponse;

                myBagResponse.Currency = await _dbContext.GlobalCurrencySettings.Select(gcs =>
                    new CurrencyDetailsResponse
                    {
                        Code = gcs.BaseCurrency.Code,
                        Name = gcs.BaseCurrency.Name,
                        Symbol = gcs.BaseCurrency.Symbol,
                        Uid = gcs.BaseCurrency.Uid,
                    }).SingleOrDefaultAsync(cancellationToken);


                if (cUser.Profile.Currency != null)
                {
                    myBagResponse.Currency = new CurrencyDetailsResponse
                    {
                        Code = cUser.Profile.Currency.Code,
                        Name = cUser.Profile.Currency.Name,
                        Symbol = cUser.Profile.Currency.Symbol,
                        Uid = cUser.Profile.Currency.Uid,
                    };
                }

                var anyProducts = await _dbContext.UserBagProducts.Where(bp => bp.UserId == cUser.Id)
                    .AnyAsync(cancellationToken);
                if (!anyProducts)
                {
                    return myBagResponse;
                }

                myBagResponse.Products = await _dbContext.UserBagProducts.Where(bp => bp.UserId == cUser.Id)
                    .Select(bp => new BagProductResponse
                    {
                        BagQuantity = bp.Quantity,
                        Uid = bp.BagProduct.Uid,
                        Name = bp.BagProduct.Name,
                        ArticleCode = bp.BagProduct.ArticleCode,
                        AffiliateId = bp.Affiliate.AffiliateId,
                        Description = bp.BagProduct.Description,
                        // MoreInfos = not needed for now
                        Price = bp.BagProduct.Price,
                        Quantity = bp.BagProduct.Quantity,
                        ProductMediaFiles = bp.BagProduct.ProductMediaFiles.Select(pmf => new MediaFileDetailsResponse
                            {
                                FileType = pmf.MediaFile.MediaFileType.ToString(),
                                Url = pmf.MediaFile.Url,
                                Priority = pmf.MediaFile.Priority,
                                Uid = pmf.MediaFile.Uid
                            })
                            // TODO: optimize this
                            
                    }).ToListAsync(cancellationToken);

                return myBagResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
