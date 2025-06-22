using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Post;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Services
{
    public class PostService : IPostService
    {
        private readonly ILogger<PostService> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileUploadService _fileUploadService;
        private readonly IConfiguration _configuration;
        private readonly IQueryHelperService _queryHelperService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly IMapper _mapper;

        public PostService(
            ILogger<PostService> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IFileUploadService fileUploadService,
            IConfiguration configuration,
            IQueryHelperService queryHelperService,
            IExchangeRateService exchangeRateService,
            IMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _fileUploadService = fileUploadService;
            _configuration = configuration;
            _queryHelperService = queryHelperService;
            _exchangeRateService = exchangeRateService;
            _mapper = mapper;
        }


        public async Task<PagingResponse<PostResponse>> GetPosts(GetPostsQueryParams queryParams)
        {
            try
            {
                IQueryable<Post> query = _dbContext.Posts.Where(p => !p.User.IsSuspended);

                var cUser = await _currentUserService.GetUserAsync(false, true);
                Store store = null;

                // Filter out posts from blocked users
                if (cUser != null)
                {
                    var blockedProfileIds = await _dbContext.UserBlocks
                        .Where(ub => ub.BlockerProfileId == cUser.Profile.Uid && ub.IsActive)
                        .Select(ub => ub.BlockedProfileId)
                        .ToListAsync();

                    query = query.Where(p => !blockedProfileIds.Contains(p.User.Profile.Uid) && p.IsActive);

                    // Filter out posts reported by current user
                    var reportedPostIds = await _dbContext.Reports
                        .Where(r => r.ReportType == ReportTypeEnum.Post 
                            && r.IsActive 
                            && r.ReportedById == cUser.Id)
                        .Select(r => r.EntityUid)
                        .ToListAsync();

                    _logger.LogInformation($"Found {reportedPostIds.Count} reported posts for user {cUser.Id}");
                    
                    if (reportedPostIds.Any())
                    {
                        query = query.Where(p => !reportedPostIds.Contains(p.Uid));
                        _logger.LogInformation("Applied reported posts filter");
                    }

                    // Filter out posts from private profiles that the current user is not following
                    var privateProfileIds = await _dbContext.Profiles
                        .Where(p => !p.ProfileSettings.IsProfilePublic)
                        .Select(p => p.Id)
                        .ToListAsync();

                    var followingProfileIds = await _dbContext.ProfileFollowers
                        .Where(pf => pf.FollowerId == cUser.Profile.Id)
                        .Select(pf => pf.ProfileId)
                        .ToListAsync();

                    query = query.Where(p => 
                        !privateProfileIds.Contains(p.User.Profile.Id) || 
                        followingProfileIds.Contains(p.User.Profile.Id));
                }
                else
                {
                    // If not logged in, only show posts from public profiles
                    query = query.Where(p => p.IsActive && p.User.Profile.ProfileSettings.IsProfilePublic);
                }

                // we calculate here, cause ef core doesnt know how to compare column value with DateTime.Now directly 
                var datetimeNow = DateTime.Now;

                if (!String.IsNullOrWhiteSpace(queryParams.Search))
                {
                    if (queryParams.Search.StartsWith("#"))
                    {
                        var searchWithoutHashtag = queryParams.Search.Replace("#", "");
                        query = query.Where(p =>
                            p.PostHashtags.Any(ph => EF.Functions.Like(ph.Hashtag.Value, $"%{searchWithoutHashtag}%")));
                    }
                    else
                    {
                        query = query.Where(p =>
                            EF.Functions.Like(p.User.UserName, $"%{queryParams.Search}%") ||
                            EF.Functions.Like(p.Store.UniqueName, $"%{queryParams.Search}%"));
                    }
                }

                if (!string.IsNullOrWhiteSpace(queryParams.Tags))
                {
                    var tagsList = queryParams.Tags.Split(",").Select(t => t.ToLower()).ToList();
                    var tagsListWithHashtag = tagsList.Select(t => $"#{t}").ToList();
                    query = query.Where(p => p.PostHashtags.Any(ph => tagsListWithHashtag.Contains(ph.Hashtag.Value.ToLower())));
                }

                //TODO rewrite this filter based on new categories structure
                /*if (!String.IsNullOrWhiteSpace(queryParams.Categories))
                {
                    var categorySlugList = queryParams.Categories.Split(',').ToList();
                    query = query.Where(p => p.PostProductTags != null && p.PostProductTags.Any(ppt =>
                        ppt.Product.ProductCategory != null &&
                        categorySlugList.Contains(ppt.Product.ProductCategory.Category.Slug)));
                }*/


                if (queryParams.ProfileType == ProfileTypeEnum.Store &&
                    !String.IsNullOrWhiteSpace(queryParams.EntityUid))
                {
                    store = await _dbContext.Stores.SingleOrDefaultAsync(s => s.Uid == queryParams.EntityUid);
                    query = query.Where(p => p.Store.Uid == queryParams.EntityUid);
                }
                else if (queryParams.ProfileType == ProfileTypeEnum.Profile &&
                         !String.IsNullOrWhiteSpace(queryParams.EntityUid))
                {
                    query = query.Where(p => p.User.Profile.Uid == queryParams.EntityUid);
                    _logger.LogInformation($"Filtering posts for profile {queryParams.EntityUid}");
                }

                //filter by hashtag 
                if (!String.IsNullOrWhiteSpace(queryParams.Hashtag))
                {
                    query = query.Where(p => p.PostHashtags.Any(ph => ph.Hashtag.Value == queryParams.Hashtag));
                }

                // Filter out posts reported by current user after profile filtering
                if (cUser != null)
                {
                    var reportedPostIds = await _dbContext.Reports
                        .Where(r => r.ReportType == ReportTypeEnum.Post 
                            && r.IsActive 
                            && r.ReportedById == cUser.Id)
                        .Select(r => r.EntityUid)
                        .ToListAsync();

                    if (reportedPostIds.Any())
                    {
                        query = query.Where(p => !reportedPostIds.Contains(p.Uid));
                    }
                }

                else if (queryParams.PostType == PostTypeEnum.Product)
                {
                    query = query.Where(p => p.PostProductTags.Any());
                }

                if (!String.IsNullOrWhiteSpace(queryParams.Order) && !String.IsNullOrWhiteSpace(queryParams.OrderBy))
                {
                    query = _queryHelperService.AppendOrderBy(query, queryParams.OrderBy, queryParams.Order);
                }
                else if (queryParams.SortingLogic == PostSortingLogicEnum.Trending)
                {
                    query = query.OrderByDescending(e => e.PostLikes.Count())
                        .ThenByDescending(e => e.Comments.Count())
                        .ThenByDescending(e => e.PostClicks.Where(pc => pc.User != null).Count())
                        .ThenByDescending(e => e.PostClicks.Where(pc => pc.User == null).Count());
                }
                else
                {
                    query = query.OrderByDescending(u => u.CreatedAt);
                }

                List<string> currencyCodes = null;
                //string storeCurrencyCode = null;
                List<ExchangeRate> exchangeRates = null;
                bool doExchangeRate = true;
                Currency currency = null;

                if (queryParams.CurrencyCode != null)
                {
                    exchangeRates = await _exchangeRateService.GetExchangeRates(currencyCodes);
                    currency = await _dbContext.Currencies.SingleOrDefaultAsync(c => c.IsActive && c.Code == queryParams.CurrencyCode);
                }
                else
                {
                    currency = await _dbContext.Currencies.SingleOrDefaultAsync(c => c.Code == _configuration["ProfileSettings:DefaultCurrencyCode"]);
                }

                var queryMapped = query
                    .Select(c => new PostResponse
                    {
                        Uid = c.Uid,
                        StoreUid = c.Store != null ? c.Store.Uid : null,
                        ProfileUid = c.Store == null ? c.User.Profile.Uid : null,
                        Text = c.Text,
                        MediaFile = _mapper.Map<MediaFileDetailsResponse>(c.MediaFile),
                        LikesCount = c.PostLikes.Count(),
                        LikedByMe = cUser != null && c.PostLikes.Any(pl => pl.LikedById == cUser.Profile.Id),
                        TaggedProductUids = c.PostProductTags.Select(ppt => ppt.Product.Uid),
                        CreatedAt = c.CreatedAt,
                        PostedByStore = c.Store != null,
                        PostProfileMentions = c.PostProfileMentions.Select(e => e.Profile.User.UserName).ToList(),
                        PostStoreMentions = c.PostStoreMentions.Select(e => e.Store.UniqueName).ToList(),
                        PostHashtags = c.PostHashtags.Select(e => e.Hashtag.Value).ToList(),
                        BookmarkedByMe = cUser != null && c.Bookmarks.Any(b => b.ProfileId == cUser.Profile.Id),
                        BookmarksCount = c.Bookmarks.Count,
                        MyStylesCount = c.PostMyStyles.Count,
                        IsMyStyle = cUser != null && c.PostMyStyles.Any(ms => ms.ProfileId == cUser.Profile.Id),
                        CommentsCount = c.Comments.Count(comment => comment.IsActive),
                        Store = c.Store != null ? new StoreBaseResponse()
                        {
                            Uid = c.Store.Uid,
                            Name = c.Store.Name,
                            ImageUrl = c.Store.ImageUrl,
                            UniqueName = c.Store.UniqueName,
                            CurrencyCode = c.Store.Currency.Code,
                            FollowedByMe = cUser != null && c.Store.StoreFollowers.Any(sf => sf.FollowerId == cUser.Profile.Id),
                        } : null,
                        PostProductTags = c.PostProductTags.Select(e => new PostProductTagResponse()
                        {
                            Product = new ProductPublicResponse()
                            {
                                Uid = e.Product.Uid,
                                Name = e.Product.Name,
                                Price = doExchangeRate
                                    ? _exchangeRateService.GetCurrencyExchangeRates(e.Product.Store.Currency.Code,
                                        queryParams.CurrencyCode, e.Product.Price, exchangeRates)
                                    : e.Product.Price,
                                CurrencyUid =  currency.Uid,
                                CurrencyCode = currency.Code,
                                StoreName = e.Product.Store.Name,
                                FeaturedImageUrl = e.Product.ProductMediaFiles
                                    .Where(pmf =>
                                        pmf.MediaFile.MediaFileType == MediaFileTypeEnum.Image &&
                                        pmf.MediaFile.Priority == 0).FirstOrDefault().MediaFile.Url
                            },
                            PositionLeftPercent = e.PositionLeftPercent,
                            PositionTopPercent = e.PositionTopPercent
                        }).ToList(),
                        Profile = c.Store == null
                            ? new ProfileBaseResponse()
                            {
                                Uid = c.User.Profile.Uid,
                                FullName = c.User.FirstName,
                                FirstName = c.User.FirstName,
                                LastName = c.User.LastName,
                                ImageUrl = c.User.Profile.ImageUrl,
                                Username = c.User.UserName,
                                DisplayName = c.User.DisplayName,
                                FollowedByMe = cUser != null
                                    ? c.User.Profile.ProfileFollowers.Any(e => e.FollowerId == cUser.Profile.Id)
                                    : false,
                            }
                            : null,
                        PostType = PostTypeEnum.Feed,
                    });

                //var queryRaw = queryMapped.ToSql();
                var list = await PagedList<PostResponse>.ToPagedListAsync(queryMapped, queryParams.PageNumber,
                    queryParams.PageSize);

                foreach (var item in list)
                {
                    if (item.Store != null)
                    {
                        if (cUser != null)
                        {
                            item.PostType = PostTypeEnum.Feed;
                        }

                        item.Store.IsMyStore = cUser?.Stores.Any(store => store.Uid == item.Store.Uid) ?? false;
                    }
                }
                var res = _mapper.Map<PagingResponse<PostResponse>>(list);
                return res;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<ToggleLikePostDto> PostToggleLike(string postUid)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                var post = await _dbContext.Posts.SingleOrDefaultAsync(p => p.Uid == postUid);

                if (cUser.Profile == null)
                {
                    throw new BadRequestException($"Profile doesnt exist for user '{cUser.Id}' .");
                }

                if (post == null)
                {
                    throw new BadRequestException($"Post with uid {postUid} doesnt exist.");
                }

                var existingPostLike = await _dbContext.PostLikes
                    .Include(pl => pl.Post)
                    .SingleOrDefaultAsync(l => l.Post.Uid == postUid && l.LikedBy.Uid == cUser.Profile.Uid);

                var likedByMe = false;
                if (existingPostLike == null)
                {
                    _dbContext.PostLikes.Add(new PostLike() { Post = post, LikedBy = cUser.Profile });
                    likedByMe = true;
                }
                else
                {
                    _dbContext.PostLikes.Remove(existingPostLike);
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);
                return new ToggleLikePostDto()
                {
                    LikedByMe = likedByMe,
                    LikesCount = await _dbContext.PostLikes
                        .Where(pl => pl.PostId == post.Id).CountAsync()
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task ToggleToMyStyle(string postUid)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                if (cUser?.Profile == null)
                {
                    throw new BadRequestException($"User {cUser?.UserName} doesnt have a profile.");
                }

                ;

                var post = await _dbContext.Posts.SingleOrDefaultAsync(p => p.Uid == postUid);
                if (post == null)
                {
                    throw new BadRequestException($"Post with uid {postUid} doesnt exist.");
                }

                var postMyStyle = await _dbContext.PostMyStyles.SingleOrDefaultAsync(pms =>
                    pms.Post.Id == post.Id && pms.Profile.Id == cUser.Profile.Id);

                if (postMyStyle == null)
                {
                    _dbContext.PostMyStyles.Add(new PostMyStyle() { Post = post, Profile = cUser.Profile });
                }
                else
                {
                    _dbContext.PostMyStyles.Remove(postMyStyle);
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<PagingResponse<PostResponse>> GetPostsMyStyle(GetPostsQueryParams queryParams)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();

                IQueryable<PostMyStyle> query = _dbContext.PostMyStyles;

                query = query.Where(pms => pms.Profile.Uid == queryParams.EntityUid && !pms.Profile.User.IsSuspended);

                // Filter out posts from blocked users
                if (cUser != null)
                {
                    var blockedProfileIds = await _dbContext.UserBlocks
                        .Where(ub => ub.BlockerProfileId == cUser.Profile.Uid && ub.IsActive)
                        .Select(ub => ub.BlockedProfileId)
                        .ToListAsync();

                    query = query.Where(pms => !blockedProfileIds.Contains(pms.Post.User.Profile.Uid) && pms.IsActive);
                }

                // Filter out reported posts
                var reportedPostIds = await _dbContext.Reports
                    .Where(r => r.ReportType == ReportTypeEnum.Post && r.IsActive)
                    .Select(r => r.EntityUid)
                    .ToListAsync();

                query = query.Where(pms => !reportedPostIds.Contains(pms.Post.Uid));

                var queryMapped = query
                    .Select(pms => new PostResponse()
                    {
                        Uid = pms.Post.Uid,
                        // TODO -> same for my styles for store
                        StoreUid = pms.Post.Store != null ? pms.Post.Store.Uid : null,
                        ProfileUid = pms.Post.Store == null ? pms.Post.User.Profile.Uid : null,
                        Text = pms.Post.Text,
                        MediaFile = _mapper.Map<MediaFileDetailsResponse>(pms.Post.MediaFile),
                        LikesCount = pms.Post.PostLikes.Count(),
                        LikedByMe = cUser != null
                            ? pms.Post.PostLikes.Any(pl => pl.LikedBy.Uid == cUser.Profile.Uid)
                            : false,
                        TaggedProductUids = pms.Post.PostProductTags.Select(ppt => ppt.Product.Uid),
                        PostProductTags = pms.Post.PostProductTags.Select(e => new PostProductTagResponse
                        {
                            PositionLeftPercent = e.PositionLeftPercent,
                            PositionTopPercent = e.PositionTopPercent,
                            Product = _mapper.Map<ProductPublicResponse>(e.Product)
                        }).ToList(),
                        Profile = pms.Post.Store == null ? new ProfileBaseResponse()
                        {
                            Username = pms.Post.User.UserName,
                            ImageUrl = pms.Post.User.Profile.ImageUrl,
                        } : null,
                        Store = pms.Post.Store == null
                            ? null
                            : new StoreBaseResponse()
                            {
                                Uid = pms.Post.Store.Uid,
                                Name = pms.Post.Store.Name,
                                UniqueName = pms.Post.Store.UniqueName,
                                CurrencyCode = pms.Post.Store.Currency.Code,
                                ImageUrl = pms.Post.Store.ImageUrl
                            },
                        CreatedAt = pms.CreatedAt,
                        PostType = PostTypeEnum.MyStyle,
                        IsMyStyle = true,
                        PostedByStore = pms.Post.Store != null
                    });

                var list = await PagedList<PostResponse>.ToPagedListAsync(queryMapped, queryParams.PageNumber,
                    queryParams.PageSize);

                var res = _mapper.Map<PagingResponse<PostResponse>>(list);
                return res;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}