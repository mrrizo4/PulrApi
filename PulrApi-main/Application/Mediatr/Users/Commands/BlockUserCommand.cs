using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.Users.Commands
{
    public class BlockUserCommand : IRequest<BlockUserResponse>
    {
        public string ProfileIdToBlock { get; set; }
    }

    public class BlockUserResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class BlockUserCommandHandler : IRequestHandler<BlockUserCommand, BlockUserResponse>
    {
        private readonly ILogger<BlockUserCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserBlockService _userBlockService;

        public BlockUserCommandHandler(
            ILogger<BlockUserCommandHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IUserBlockService userBlockService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _userBlockService = userBlockService;
        }

        public async Task<BlockUserResponse> Handle(BlockUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = await _currentUserService.GetUserAsync();
                var currentUserProfile = await _dbContext.Profiles
                    .FirstOrDefaultAsync(p => p.UserId == currentUser.Id, cancellationToken);

                if (currentUserProfile == null)
                {
                    return new BlockUserResponse
                    {
                        Success = false,
                        Message = "Current user profile not found."
                    };
                }

                // Check if user is trying to block themselves
                if (currentUserProfile.Uid == request.ProfileIdToBlock)
                {
                    return new BlockUserResponse
                    {
                        Success = false,
                        Message = "You cannot block yourself."
                    };
                }

                // Check if block already exists
                var existingBlock = await _dbContext.UserBlocks
                    .FirstOrDefaultAsync(b => 
                        b.BlockerProfileId == currentUserProfile.Uid && 
                        b.BlockedProfileId == request.ProfileIdToBlock,
                        cancellationToken);

                if (existingBlock != null)
                {
                    return new BlockUserResponse
                    {
                        Success = false,
                        Message = "User is already blocked."
                    };
                }

                // Create new block
                var block = new UserBlock
                {
                    BlockerProfileId = currentUserProfile.Uid,
                    BlockedProfileId = request.ProfileIdToBlock,
                    Uid = Guid.NewGuid().ToString(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.UserBlocks.Add(block);

                // Handle all the effects of blocking (unfollow, remove likes, etc.)
                await _userBlockService.HandleUserBlock(
                    currentUserProfile.Uid,
                    request.ProfileIdToBlock,
                    cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                return new BlockUserResponse
                {
                    Success = true,
                    Message = "User blocked successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking user");
                return new BlockUserResponse
                {
                    Success = false,
                    Message = "An error occurred while blocking the user."
                };
            }
        }
    }
} 