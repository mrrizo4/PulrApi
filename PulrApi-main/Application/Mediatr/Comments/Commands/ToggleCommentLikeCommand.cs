using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Comments.Queries;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Comments.Commands;

public class ToggleCommentLikeCommand : IRequest<CommentToggleLikeResponse>
{
    public string Uid { get; set; }
}

public class ToggleCommentLikeCommandHandler : IRequestHandler<ToggleCommentLikeCommand, CommentToggleLikeResponse>
{
    private readonly ILogger<ToggleCommentLikeCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public ToggleCommentLikeCommandHandler(ILogger<ToggleCommentLikeCommandHandler> logger, ICurrentUserService currentUserService, IApplicationDbContext dbContext)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
    }

    public async Task<CommentToggleLikeResponse> Handle(ToggleCommentLikeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cUser = await _currentUserService.GetUserAsync();


            if (cUser.Profile == null)
                throw new BadRequestException($"Comment doesnt exist for user '{cUser.Id}' .");

            var comment = await _dbContext.Comments.SingleOrDefaultAsync(p => p.Uid == request.Uid, cancellationToken);

            if (comment == null)
                throw new BadRequestException($"Comment doesn't exist.");


            var existingCommentLike = await _dbContext.CommentLikes
                .Include(pl => pl.Comment)
                .SingleOrDefaultAsync(l => l.Comment.Uid == request.Uid && l.LikedBy.Uid == cUser.Profile.Uid, cancellationToken);

            var likedByMe = false;
            if (existingCommentLike == null)
            {
                _dbContext.CommentLikes.Add(new CommentLike { Comment = comment, LikedBy = cUser.Profile });
                likedByMe = true;
            }
            else
            {
                _dbContext.CommentLikes.Remove(existingCommentLike);
            }

            await _dbContext.SaveChangesAsync(CancellationToken.None);
            return new CommentToggleLikeResponse
            {
                LikesCount = await _dbContext.CommentLikes.Where(c => c.CommentId == comment.Id).CountAsync(cancellationToken),
                LikedByMe = likedByMe
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error toggling like for a comment");
            throw;
        }
    }
}