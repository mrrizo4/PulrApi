using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Posts.Queries;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Post;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Posts.Queries
{
    public class GetPostQuery : IRequest<PostDetailsResponse>
    {
        [Required]
        public string Uid { get; set; }
        public string CurrencyCode { get; set; }
    }

    public class GetPostQueryHandler : IRequestHandler<GetPostQuery, PostDetailsResponse>
    {
        private readonly ILogger<GetPostQueryHandler> _logger;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;
        private readonly IExchangeRateService _exchangeRateService;

        public GetPostQueryHandler(ILogger<GetPostQueryHandler> logger, IMapper mapper, ICurrentUserService currentUserService, IApplicationDbContext dbContext, IExchangeRateService exchangeRateService)
        {
            _logger = logger;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
            _exchangeRateService = exchangeRateService;
        }

        public async Task<PostDetailsResponse> Handle(GetPostQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var uid = request.Uid;
                var currencyCode = request.CurrencyCode != null ? await _dbContext.Currencies.SingleOrDefaultAsync(c => c.Code == request.CurrencyCode, cancellationToken) : null;

                var cUser = await _currentUserService.GetUserAsync();

                var queryPost = _dbContext.Posts.Where(p => p.Uid == uid && !p.User.IsSuspended);

                var post = await queryPost
                    .Include(p => p.User).ThenInclude(u => u.Profile)
                    .Include(p => p.PostProductTags)
                    .ThenInclude(ppt => ppt.Product)
                    .ThenInclude(ppp => ppp.Store).ThenInclude(s => s.Currency)
                    .Include(p => p.PostClicks).SingleOrDefaultAsync();
                if (post == null)
                {
                    throw new BadRequestException($"Post with uid {uid} not found.");
                }

                var existingPostClick = await _dbContext.PostClicks.SingleOrDefaultAsync(pc => pc.Post.Id == post.Id && pc.User == cUser);
                if (existingPostClick != null)
                {
                    existingPostClick.Count += 1;
                }
                else
                {
                    post.PostClicks.Add(new PostClick() { Post = post, User = cUser, Count = 1 });
                }
                await _dbContext.SaveChangesAsync(CancellationToken.None);


                List<string> currencyCodes = null;
                List<string> storeCurrencyCodes = null;
                List<ExchangeRate> exchangeRates = null;
                bool doExchangeRate = false;

                if (currencyCode != null && post.PostProductTags.Any())
                {
                    storeCurrencyCodes = post.PostProductTags.DistinctBy(ppt => ppt.Product.Store.Currency.Code).Select(ppt => ppt.Product.Store.Currency.Code).ToList();
                    currencyCodes = new List<string>() { currencyCode.Code };
                    currencyCodes.AddRange(storeCurrencyCodes);
                    exchangeRates = await _exchangeRateService.GetExchangeRates(currencyCodes);
                    doExchangeRate = currencyCodes != null && storeCurrencyCodes.Any() && exchangeRates != null;
                }


                var postRes = await queryPost.Select(post => new PostDetailsResponse()
                {
                    Uid = post.Uid,
                    Text = post.Text,
                    CreatedAt = post.CreatedAt,
                    LikesCount = post.PostLikes.Count(),
                    CommentsCount = post.Comments.Count(c => c.IsActive),
                    BookmarksCount = post.Bookmarks.Count,
                    BookmarkedByMe = cUser != null && post.Bookmarks.Any(b => b.ProfileId == cUser.Profile.Id && b.IsActive),
                    MyStylesCount = post.PostMyStyles.Count,
                    LikedByMe = cUser != null ? post.PostLikes.Any(pl => pl.LikedById == cUser.Profile.Id) : false,
                    MediaFile = _mapper.Map<MediaFileDetailsResponse>(post.MediaFile),
                    PostHashtags = post.PostHashtags.Select(e => e.Hashtag.Value).ToList(),
                    PostProfileMentions = post.PostProfileMentions.Select(e => e.Profile.User.UserName).ToList(),
                    PostStoreMentions = post.PostStoreMentions.Select(e => e.Store.UniqueName).ToList(),
                    PostType = PostTypeEnum.Feed,
                    ProfileUid = post.Store == null ? post.User.Profile.Uid : null,
                    Profile = new ProfileDetailsResponse()
                    {
                        Uid = post.User.Profile.Uid,
                        Username = post.User.UserName,
                        FullName = post.User.FirstName,
                        FirstName = post.User.FirstName,
                        DisplayName = post.User.DisplayName,
                        LastName = post.User.LastName,
                        ImageUrl = post.User.Profile.ImageUrl,
                        FollowedByMe = cUser != null ? post.User.Profile.ProfileFollowers.Any(pf => pf.FollowerId == cUser.Profile.Id) : false,
                    },
                    Store = new StoreDetailsResponse()
                    {
                        Uid = post.PostProductTags.Select(t => t.Product.Store).FirstOrDefault().Uid,
                        Name = post.PostProductTags.Select(t => t.Product.Store).FirstOrDefault().Name,
                        UniqueName = post.PostProductTags.Select(t => t.Product.Store).FirstOrDefault().UniqueName,
                        ImageUrl = post.PostProductTags.Select(t => t.Product.Store).FirstOrDefault().ImageUrl,
                        FollowedByMe = cUser != null ? post.Store.StoreFollowers.Any(sf => sf.FollowerId == cUser.Profile.Id) : false,
                    },
                    StoreUid = post.Store != null ? post.Store.Uid : null,
                    PostedByStore = post.Store != null,
                    PostProductTags = post.PostProductTags.Select(e => new PostProductTagResponse()
                    {
                        Product = new ProductPublicResponse()
                        {
                            Uid = e.Product.Uid,
                            Name = e.Product.Name,
                            Price = doExchangeRate ? _exchangeRateService.GetCurrencyExchangeRates(e.Product.Store.Currency.Code, currencyCode.Code, e.Product.Price, exchangeRates) : e.Product.Price,
                            CurrencyUid = doExchangeRate ? currencyCode.Uid : e.Product.Store.Currency.Uid,
                            CurrencyCode = doExchangeRate ? currencyCode.Code : e.Product.Store.Currency.Code,
                            StoreName = e.Product.Store.Name,
                            FeaturedImageUrl = e.Product.ProductMediaFiles.Where(pmf => pmf.MediaFile.MediaFileType == MediaFileTypeEnum.Image && pmf.MediaFile.Priority == 0).FirstOrDefault().MediaFile.Url
                        },
                        PositionLeftPercent = e.PositionLeftPercent,
                        PositionTopPercent = e.PositionTopPercent
                    }).ToList()
                }).SingleOrDefaultAsync(cancellationToken);


                if (cUser?.Profile != null)
                {
                    postRes.IsMyStyle = await _dbContext.PostMyStyles.AnyAsync(pms => pms.Post.Uid == postRes.Uid && pms.Profile.Id == cUser.Profile.Id);
                }

                if (postRes.PostProductTags.Any())
                {
                    postRes.TaggedProductUids = post.PostProductTags.Select(ppt => ppt.Product.Uid).ToList();
                }

                return postRes;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
