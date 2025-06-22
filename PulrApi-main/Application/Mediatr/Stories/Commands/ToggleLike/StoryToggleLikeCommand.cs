using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Stories.Queries;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Stories.Commands.ToggleLike;

public class StoryToggleLikeCommand : IRequest<StoryToggleLikeResponse>
{
    public string StoryUid { get; set; }
}

public class StoryToggleLikeCommandHandler : IRequestHandler<StoryToggleLikeCommand, StoryToggleLikeResponse>
{
    private readonly ILogger<StoryToggleLikeCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public StoryToggleLikeCommandHandler(ILogger<StoryToggleLikeCommandHandler> logger, ICurrentUserService currentUserService, IApplicationDbContext dbContext)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
    }

    public async Task<StoryToggleLikeResponse> Handle(StoryToggleLikeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();

            if (currentUser.Profile == null)
                throw new BadRequestException($"Profile doesn't exist for user");


            var story = await _dbContext.Stories.SingleOrDefaultAsync(s => s.IsActive && s.Uid == request.StoryUid, cancellationToken);

            if (story == null)
                throw new NotFoundException("Story not found");

            var existingStoryLike = await _dbContext.StoryLikes.Include(sl => sl.Story)
                .SingleOrDefaultAsync(l => l.Story.Uid == request.StoryUid && l.LikedBy.Uid == currentUser.Profile.Uid, cancellationToken);

            var likedByMe = false;
            if (existingStoryLike == null)
            {
                _dbContext.StoryLikes.Add(new StoryLike
                {
                    Story = story,
                    LikedBy = currentUser.Profile
                });
                likedByMe = true;
            }
            else
            {
                _dbContext.StoryLikes.Remove(existingStoryLike);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return new StoryToggleLikeResponse
            {
                LikedByMe = likedByMe,
                LikesCount = await _dbContext.StoryLikes
                    .Where(pl => pl.StoryId == story.Id).CountAsync(cancellationToken)
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error toggling like for story");
            throw;
        }
    }
}