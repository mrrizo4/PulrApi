using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Comments.Commands
{
    public class UpdateCommentCommand : IRequest<CommentResponse>
    {
        [Required]
        public string CommentUid { get; set; }

        [Required]
        public string Content { get; set; }
    }

    public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, CommentResponse>
    {
        private readonly ILogger<UpdateCommentCommandHandler> _logger;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public UpdateCommentCommandHandler(ILogger<UpdateCommentCommandHandler> logger, IMapper mapper,
            ICurrentUserService currentUserService, IApplicationDbContext dbContext)
        {
            _logger = logger;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<CommentResponse> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                if (cUser.Profile == null)
                {
                    throw new BadRequestException($"User {cUser.UserName} doesnt have a profile.");
                }

                ;

                var comment = await _dbContext.Comments
                    .AsSplitQuery()
                    .Include(c => c.Post)
                    .Include(c => c.Product)
                    .Include(c => c.CommentedBy)
                    .Include(c => c.CommentLikes)
                    .SingleOrDefaultAsync(c => c.Uid == request.CommentUid && c.CommentedBy.Id == cUser.Profile.Id, cancellationToken);
                if (comment == null)
                {
                    throw new BadRequestException($"Comment with uid {request.CommentUid} doesnt exist.");
                }

                comment.Content = request.Content;
                await _dbContext.SaveChangesAsync(cancellationToken);
                var commentResponse = _mapper.Map<CommentResponse>(comment);

                commentResponse.PostUid = comment.Post?.Uid;
                commentResponse.AuthorProfileImageUrl = comment.Product?.Uid;
                commentResponse.AuthorProfileImageUrl = comment.CommentedBy.ImageUrl;
                commentResponse.DisplayName = comment.CommentedBy.User.DisplayName;
                commentResponse.LikedByMe = cUser.Profile != null && comment.CommentLikes.Any(l => l.LikedById == cUser.Profile.Id);
                return commentResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}