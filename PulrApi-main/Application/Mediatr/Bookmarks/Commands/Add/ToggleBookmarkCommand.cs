using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Bookmarks.Queries;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Bookmarks.Commands.Add;

public class ToggleBookmarkCommand : IRequest<BookmarkDto>
{
    public string PostUid { get; set; }
    public BookmarkActionEnum Action { get; set; }
}

public class ToggleBookmarkCommandHandler : IRequestHandler<ToggleBookmarkCommand, BookmarkDto>
{
    private readonly ILogger<ToggleBookmarkCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public ToggleBookmarkCommandHandler(
        ILogger<ToggleBookmarkCommandHandler> logger,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
    }

    public async Task<BookmarkDto> Handle(ToggleBookmarkCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await _currentUserService.GetUserAsync();
        var post = await _dbContext.Posts.SingleOrDefaultAsync(p => p.IsActive && p.Uid == request.PostUid, cancellationToken);

        if (post == null)
            throw new NotFoundException("Post not found");

        if (request.Action == BookmarkActionEnum.Bookmark)
            return await ToggleBookmark(cancellationToken, currentUser, post);

        if (request.Action == BookmarkActionEnum.MyStyles)
            return await ToggleMyStyles(cancellationToken, post, currentUser);

        return new BookmarkDto();
    }

    private async Task<BookmarkDto> ToggleBookmark(CancellationToken cancellationToken, User currentUser, Post post)
    {
        try
        {
            var bookmark = await _dbContext.Bookmarks.Include(b => b.Post)
                .SingleOrDefaultAsync(b => b.IsActive
                                           && b.ProfileId == currentUser.Profile.Id
                                           && b.PostId == post.Id, cancellationToken);

            if (bookmark != null)
            {
                _dbContext.Bookmarks.Remove(bookmark);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return new BookmarkDto
                {
                    Count = await _dbContext.Bookmarks.CountAsync(b => b.IsActive
                                                                       && b.PostId == post.Id, cancellationToken),
                    Action = BookmarkActionEnum.Bookmark.ToString(),
                    BookmarkedByMe = false,
                    IsMyStyle = await _dbContext.PostMyStyles.AnyAsync(ms => ms.PostId == post.Id && ms.ProfileId == currentUser.Profile.Id, cancellationToken),
                    PostUid = bookmark.Post.Uid
                };
            }

            _dbContext.Bookmarks.Add(new Bookmark
            {
                ProfileId = currentUser.Profile.Id,
                PostId = post.Id
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new BookmarkDto
            {
                Count = await _dbContext.Bookmarks.CountAsync(b => b.IsActive
                                                                   && b.PostId == post.Id, cancellationToken),
                Action = BookmarkActionEnum.Bookmark.ToString(),
                BookmarkedByMe = true,
                IsMyStyle = await _dbContext.PostMyStyles.AnyAsync(ms => ms.PostId == post.Id && ms.ProfileId == currentUser.Profile.Id, cancellationToken),
                PostUid = post.Uid
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error toggling bookmark");
            throw;
        }
    }

    private async Task<BookmarkDto> ToggleMyStyles(CancellationToken cancellationToken, Post post, User currentUser)
    {
        try
        {
            var myStyles = await _dbContext.PostMyStyles.Include(b => b.Post)
                .SingleOrDefaultAsync(ms =>
                    ms.PostId == post.Id
                    && ms.Profile.Id == currentUser.Profile.Id, cancellationToken);

            if (myStyles != null)
            {
                _dbContext.PostMyStyles.Remove(myStyles);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return new BookmarkDto
                {
                    Count = await _dbContext.PostMyStyles.CountAsync(ms =>
                        ms.PostId == post.Id, cancellationToken),
                    Action = BookmarkActionEnum.MyStyles.ToString(),
                    BookmarkedByMe = await _dbContext.Bookmarks.AnyAsync(b => b.PostId == post.Id && b.ProfileId == currentUser.Profile.Id && b.IsActive, cancellationToken),
                    IsMyStyle = false,
                    PostUid = post.Uid
                };
            }

            _dbContext.PostMyStyles.Add(new PostMyStyle
            {
                ProfileId = currentUser.Profile.Id,
                PostId = post.Id
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new BookmarkDto
            {
                Count = await _dbContext.PostMyStyles.CountAsync(ms =>
                    ms.PostId == post.Id, cancellationToken),
                Action = BookmarkActionEnum.MyStyles.ToString(),
                BookmarkedByMe = await _dbContext.Bookmarks.AnyAsync(b => b.PostId == post.Id && b.ProfileId == currentUser.Profile.Id && b.IsActive, cancellationToken),
                IsMyStyle = true,
                PostUid = post.Uid
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error toggling my styles post");
            throw;
        }
    }
}