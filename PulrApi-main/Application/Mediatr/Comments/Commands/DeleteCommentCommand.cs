using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Comments.Commands;
using System.Linq;

namespace Core.Application.Mediatr.Comments.Commands
{
    public class DeleteCommentCommand : IRequest<DeleteCommentResponse>
    {
        [Required]
        public string CommentUid { get; set; }
    }

    public class DeleteCommentResponse
    {
        public int TotalCommentsCount { get; set; }
    }

    public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, DeleteCommentResponse>
    {
        private readonly ILogger<DeleteCommentCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public DeleteCommentCommandHandler(ILogger<DeleteCommentCommandHandler> logger, ICurrentUserService currentUserService, IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<DeleteCommentResponse> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
        {
            //try
            //{
            //    var cUser = await _currentUserService.GetUserAsync();

            //    // First get the parent comment to get post/product info
            //    var parentComment = await _dbContext.Comments
            //        .Include(c => c.Post)
            //        .Include(c => c.Product)
            //        .SingleOrDefaultAsync(c => c.Uid == request.CommentUid && c.CommentedBy.Id == cUser.Profile.Id, cancellationToken);

            //    if (parentComment == null) 
            //    { 
            //        throw new BadRequestException($"Comment with uid '{request.CommentUid}' doesnt exist"); 
            //    }

            //    // Store post/product info before deletion
            //    var postUid = parentComment.Post?.Uid;
            //    var productUid = parentComment.Product?.Uid;

            //    // Get all child comments and their likes
            //    var childComments = await _dbContext.Comments
            //        .Include(c => c.CommentLikes)
            //        .Where(c => c.ParentComment.Uid == request.CommentUid)
            //        .ToListAsync(cancellationToken);

            //    // Delete likes for child comments
            //    foreach (var childComment in childComments)
            //    {
            //        _dbContext.CommentLikes.RemoveRange(childComment.CommentLikes);
            //    }
            //    await _dbContext.SaveChangesAsync(cancellationToken);

            //    // Delete child comments
            //    _dbContext.Comments.RemoveRange(childComments);
            //    await _dbContext.SaveChangesAsync(cancellationToken);

            //    // Delete likes for parent comment
            //    _dbContext.CommentLikes.RemoveRange(parentComment.CommentLikes);
            //    await _dbContext.SaveChangesAsync(cancellationToken);

            //    // Finally delete the parent comment
            //    _dbContext.Comments.Remove(parentComment);
            //    await _dbContext.SaveChangesAsync(cancellationToken);

            //    // Get total comments count for the post or product
            //    var totalCommentsCount = await _dbContext.Comments
            //        .Where(c => (postUid != null && c.Post.Uid == postUid) || 
            //                   (productUid != null && c.Product.Uid == productUid))
            //        .CountAsync(cancellationToken);

            //    return new DeleteCommentResponse
            //    {
            //        TotalCommentsCount = totalCommentsCount
            //    };
            //}
            //catch (Exception e)
            //{
            //    _logger.LogError(e, e.Message);
            //    throw;
            //}
            try
            {
                var cUser = await _currentUserService.GetUserAsync();

                // Get comment with all related data in one query
                var comment = await _dbContext.Comments
                    .Include(c => c.Post)
                    .Include(c => c.Product)
                    .Include(c => c.CommentLikes)
                    .Include(c => c.Replies)
                        .ThenInclude(r => r.CommentLikes)
                    .SingleOrDefaultAsync(c => c.Uid == request.CommentUid &&
                                             c.CommentedBy.Id == cUser.Profile.Id,
                                             cancellationToken);

                if (comment == null)
                {
                    throw new BadRequestException($"Comment with uid '{request.CommentUid}' doesnt exist");
                }

                // Store info for total count
                var postUid = comment.Post?.Uid;
                var productUid = comment.Product?.Uid;

                try
                {
                    // 1. Delete all reply likes first
                    if (comment.Replies != null)
                    {
                        foreach (var reply in comment.Replies)
                        {
                            if (reply.CommentLikes != null)
                            {
                                _dbContext.CommentLikes.RemoveRange(reply.CommentLikes);
                            }
                        }
                    }

                    // 2. Delete all replies
                    if (comment.Replies != null)
                    {
                        _dbContext.Comments.RemoveRange(comment.Replies);
                    }

                    // 3. Delete main comment's likes
                    if (comment.CommentLikes != null)
                    {
                        _dbContext.CommentLikes.RemoveRange(comment.CommentLikes);
                    }

                    // 4. Delete the main comment
                    _dbContext.Comments.Remove(comment);

                    // Save all changes at once
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting comment and its relations");
                    throw;
                }

                // Get updated total count
                var totalCommentsCount = await _dbContext.Comments
                    .Where(c => (postUid != null && c.Post.Uid == postUid) ||
                               (productUid != null && c.Product.Uid == productUid))
                    .CountAsync(cancellationToken);

                return new DeleteCommentResponse { TotalCommentsCount = totalCommentsCount };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
