using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Constants;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Post;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Bookmarks.Queries;

public class GetBookmarkedPostsForProfileQuery : PagingParamsRequest, IRequest<PagingResponse<PostResponse>>
{
}

public class GetBookmarkedPostsForProfileQueryHandler : IRequestHandler<GetBookmarkedPostsForProfileQuery, PagingResponse<PostResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<GetBookmarkedPostsForProfileQueryHandler> _logger;

    public GetBookmarkedPostsForProfileQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IMapper mapper,
        UserManager<User> userManager,
        ILogger<GetBookmarkedPostsForProfileQueryHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<PagingResponse<PostResponse>> Handle(GetBookmarkedPostsForProfileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();

            var postsQuery = _dbContext.Bookmarks
                .AsSplitQuery()
                .OrderByDescending(b => b.CreatedAt)
                .Where(b => b.IsActive && b.ProfileId == currentUser.Profile.Id)
                .Select(c => new PostResponse
                {
                    Uid = c.Post.Uid,
                    Text = c.Post.Text,
                    MediaFile = _mapper.Map<MediaFileDetailsResponse>(c.Post.MediaFile),
                    LikesCount = c.Post.PostLikes.Count(),
                    LikedByMe = currentUser != null && c.Post.PostLikes.Any(pl => pl.LikedById == currentUser.Profile.Id),
                    TaggedProductUids = c.Post.PostProductTags.Select(ppt => ppt.Product.Uid),
                    CreatedAt = c.Post.CreatedAt,
                    PostedByStore = c.Post.Store != null,
                    BookmarksCount = c.Post.Bookmarks.Count,
                    BookmarkedByMe = true,   
                    PostType = PostTypeEnum.Bookmark,
                    MyStylesCount = c.Post.PostMyStyles.Count,
                    StoreUid = c.Post.Store != null ? c.Post.Store.Uid : null,
                    Store = c.Post.Store == null
                        ? null
                        : new StoreBaseResponse
                        {
                            Uid = c.Post.Store.Uid,
                            Name = c.Post.Store.Name,
                            ImageUrl = c.Post.Store.ImageUrl,
                            UniqueName = c.Post.Store.UniqueName,
                            CurrencyCode = c.Post.Store.Currency.Code,
                            FollowedByMe = currentUser != null &&
                                           c.Post.Store.StoreFollowers.Any(sf => sf.FollowerId == currentUser.Profile.Id),
                        },
                    ProfileUid = c.Post.Store == null ? currentUser.Profile.Uid : null,
                    Profile = c.Post.Store == null
                        ? new ProfileBaseResponse
                        {
                            Uid = c.Post.User.Profile.Uid,
                            FullName = c.Post.User.FirstName,
                            FirstName = c.Post.User.FirstName,
                            LastName = c.Post.User.LastName,
                            IsStore = c.Post.Store != null,
                            ImageUrl = c.Post.User.Profile.ImageUrl,
                            Username = c.Post.User.UserName,
                            FollowedByMe = currentUser != null &&
                                           c.Post.User.Profile.ProfileFollowers.Any(e =>
                                               e.FollowerId == currentUser.Profile.Id),
                        }
                        : null,
                    //CommentsCount = c.Post.Comments.Count
                });

            var list = await PagedList<PostResponse>.ToPagedListAsync(postsQuery, request.PageNumber,
                request.PageSize);

            foreach (var post in list)
            {
                post.Profile.IsInfluencer = await _userManager.IsInRoleAsync(new User { Id = post.Profile.UserId }, PulrRoles.Influencer);
            }

            var bookmarksPagedResponse = _mapper.Map<PagingResponse<PostResponse>>(list);
            bookmarksPagedResponse.ItemIds = bookmarksPagedResponse.Items.Select(item => item.Uid).ToList();
            return bookmarksPagedResponse;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting bookmarked posts");
            throw;
        }
    }
}