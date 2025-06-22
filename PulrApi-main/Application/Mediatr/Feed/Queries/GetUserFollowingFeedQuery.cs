using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Constants;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Post;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Feed.Queries;

public class GetUserFollowingFeedQuery : PagingParamsRequest, IRequest<PagingResponse<PostResponse>>
{
    public string StoreUid { get; set; }
    public string CategoryUid { get; set; }
    public string PostUid { get; set; }
    public string CurrencyCode { get; set; }
}

public class GetUserFollowingFeedQueryHandler : IRequestHandler<GetUserFollowingFeedQuery, PagingResponse<PostResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;

    public GetUserFollowingFeedQueryHandler(
        IApplicationDbContext dbContext, 
        UserManager<User> userManager,
        IExchangeRateService exchangeRateService,
        ICurrentUserService currentUserService,
        ILogger<GetUserFollowingFeedQueryHandler> logger, IMapper mapper)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _exchangeRateService = exchangeRateService;
        _currentUserService = currentUserService;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<PagingResponse<PostResponse>> Handle(GetUserFollowingFeedQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();
            currentUser.Profile = await _dbContext.Profiles
                .SingleOrDefaultAsync(p => p.IsActive && p.UserId == currentUser.Id, cancellationToken);

            var myStyles = _dbContext.PostMyStyles.Where(ms => ms.ProfileId == currentUser.Profile.Id)
                .Select(ms => ms.Post);

            var followersQuery = _dbContext.ProfileFollowers.Where(pf =>
                !pf.Profile.User.IsSuspended && pf.FollowerId == currentUser.Profile.Id);

            var storeFollowersQuery = _dbContext.StoreFollowers.Where(sf => sf.FollowerId == currentUser.Profile.Id);

            var postsIdStoreMentionsQuery =
                _dbContext.PostStoreMentions
                    .Where(psm => storeFollowersQuery.Select(sf => sf.StoreId).Contains(psm.PostId))
                    .Select(p => p.PostId);

            var postProductTagsQuery = _dbContext.PostProductTags
                .Where(ppt =>
                    ppt.Product.Store.StoreFollowers.Select(sf => sf.FollowerId).Contains(currentUser.Profile.Id))
                .Select(ppt => ppt.PostId);

            var postsQuery = _dbContext.Posts
                .OrderByDescending(p => p.CreatedAt)
                .Include(p => p.Bookmarks)
                .Where(p => p.IsActive
                            && p.User.Id != currentUser.Id
                            && !p.User.IsSuspended
                            && (followersQuery.Select(f => f.ProfileId).Contains(p.User.Profile.Id)
                                || postProductTagsQuery.Contains(p.Id)
                                || postsIdStoreMentionsQuery.Contains(p.Id)));

            // Filter out posts from blocked users
            var blockedProfileIds = await _dbContext.UserBlocks
                .Where(ub => ub.BlockerProfileId == currentUser.Profile.Uid)
                .Select(ub => ub.BlockedProfileId)
                .ToListAsync(cancellationToken);

            postsQuery = postsQuery.Where(p => !blockedProfileIds.Contains(p.User.Profile.Uid));

            // Filter out reported posts
            var reportedPostIds = await _dbContext.Reports
                .Where(r => r.ReportType == ReportTypeEnum.Post)
                .Select(r => r.EntityUid)
                .ToListAsync(cancellationToken);

            postsQuery = postsQuery.Where(p => !reportedPostIds.Contains(p.Uid));

            // Filter out posts from private profiles that the current user is not following
            var privateProfileIds = await _dbContext.Profiles
                .Where(p => !p.ProfileSettings.IsProfilePublic)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            var followingProfileIds = await _dbContext.ProfileFollowers
                .Where(pf => pf.FollowerId == currentUser.Profile.Id)
                .Select(pf => pf.ProfileId)
                .ToListAsync(cancellationToken);

            postsQuery = postsQuery.Where(p => 
                !privateProfileIds.Contains(p.User.Profile.Id) || 
                followingProfileIds.Contains(p.User.Profile.Id));

            if (!String.IsNullOrWhiteSpace(request.Search))
            {
                if (request.Search.StartsWith("#"))
                {
                    var searchWithoutHashtag = request.Search.Replace("#", "");
                    postsQuery = postsQuery.Where(p =>
                        p.PostHashtags.Any(ph => EF.Functions.Like(ph.Hashtag.Value, $"%{searchWithoutHashtag}%")));
                }
                else
                {
                    postsQuery = postsQuery.Where(p =>
                        EF.Functions.Like(p.User.UserName, $"%{request.Search}%") ||
                        EF.Functions.Like(p.Store.UniqueName, $"%{request.Search}%"));
                }
            }
            
            List<string> currencyCodes = null;
            List<ExchangeRate> exchangeRates = null;
            if (request.CurrencyCode != null)
                exchangeRates = await _exchangeRateService.GetExchangeRates(currencyCodes);

            var queryMapped = postsQuery
                .Select(p => new PostResponse()
                {
                    Uid = p.Uid,
                    ProfileUid = p.User.Profile.Uid,
                    Text = p.Text,
                    MediaFile = _mapper.Map<MediaFileDetailsResponse>(p.MediaFile),
                    LikesCount = p.PostLikes.Count(),
                    LikedByMe = currentUser != null && p.PostLikes.Any(pl => pl.LikedById == currentUser.Profile.Id),
                    TaggedProductUids = p.PostProductTags.Select(ppt => ppt.Product.Uid),
                    CreatedAt = p.CreatedAt,
                    PostedByStore = p.Store != null,
                    Store = p.Store == null
                        ? null
                        : new StoreBaseResponse()
                        {
                            Uid = p.Store.Uid,
                            Name = p.Store.Name,
                            ImageUrl = p.Store.ImageUrl,
                            UniqueName = p.Store.UniqueName,
                            CurrencyCode = p.Store.Currency.Code,
                            FollowedByMe = currentUser != null &&
                                           p.Store.StoreFollowers.Any(sf => sf.FollowerId == currentUser.Profile.Id),
                        },
                    Profile = p.Store == null
                        ? new ProfileBaseResponse()
                        {
                            Uid = p.Uid,
                            UserId = p.User.Profile.Uid,
                            FullName = p.User.FirstName,
                            FirstName = p.User.FirstName,
                            LastName = p.User.LastName,
                            IsStore = p.Store != null,
                            ImageUrl = p.User.Profile.ImageUrl,
                            Username = p.User.UserName,
                            FollowedByMe = currentUser != null &&
                                           p.User.Profile.ProfileFollowers.Any(e =>
                                               e.FollowerId == currentUser.Profile.Id),
                        }
                        : null,
                    PostProductTags = p.PostProductTags.Select(pt => new PostProductTagResponse()
                    {
                        Product = new ProductPublicResponse()
                        {
                            Uid = pt.Product.Uid,
                            Name = pt.Product.Name,
                            Price = _exchangeRateService.GetCurrencyExchangeRates(pt.Product.Store.Currency.Code,
                                    request.CurrencyCode, pt.Product.Price, exchangeRates),
                            CurrencyCode = request.CurrencyCode ?? pt.Product.Store.Currency.Code,
                            StoreName = pt.Product.Store.Name,
                            FeaturedImageUrl = pt.Product.ProductMediaFiles
                                .Where(pmf =>
                                    pmf.MediaFile.MediaFileType == MediaFileTypeEnum.Image &&
                                    pmf.MediaFile.Priority == 0).FirstOrDefault().MediaFile.Url
                        },
                        PositionLeftPercent = pt.PositionLeftPercent,
                        PositionTopPercent = pt.PositionTopPercent
                    }).ToList(),
                    CommentsCount = p.Comments.Count,
                    BookmarkedByMe = currentUser != null && p.Bookmarks.Any(b => b.ProfileId == currentUser.Profile.Id),
                    IsMyStyle = currentUser != null && p.PostMyStyles.Any(b => b.ProfileId == currentUser.Profile.Id),
                    BookmarksCount = p.Bookmarks.Count,
                    MyStylesCount = p.PostMyStyles.Count,
                    PostType = currentUser != null && p.PostMyStyles.Any(b => b.ProfileId == currentUser.Profile.Id)
                        ? PostTypeEnum.MyStyle
                        : PostTypeEnum.Feed
                });


            var list = await PagedList<PostResponse>.ToPagedListAsync(queryMapped, request.PageNumber,
                request.PageSize);

            foreach (var post in list)
            {
                if(post.Profile != null)
                {
                    post.Profile.IsInfluencer = await _userManager.IsInRoleAsync(new User { Id = post.Profile.UserId }, PulrRoles.Influencer);
                }
                post.ShareCount = await _dbContext.Posts.CountAsync(p => p.IsActive && p.SharedPost.Uid == post.Uid, cancellationToken);
                if (currentUser != null && currentUser.Profile != null)
                {
                    post.SharedByMe = await _dbContext.Posts.AnyAsync(p => p.IsActive && p.SharedPost.User.Profile.Id == currentUser.Profile.Id, cancellationToken);
                }
            }

            var postsPagedResponse = _mapper.Map<PagingResponse<PostResponse>>(list);
            postsPagedResponse.ItemIds = postsPagedResponse.Items.Select(item => item.Uid).ToList();
            return postsPagedResponse;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting user feed with message: {message}", e.Message);
            throw;
        }
    }
}