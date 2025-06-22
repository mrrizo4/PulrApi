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
    public class UnblockUserCommand : IRequest<UnblockUserResponse>
    {
        public string ProfileIdToUnblock { get; set; }
    }

    public class UnblockUserResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class UnblockUserCommandHandler : IRequestHandler<UnblockUserCommand, UnblockUserResponse>
    {
        private readonly ILogger<UnblockUserCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserBlockService _userBlockService;

        public UnblockUserCommandHandler(
            ILogger<UnblockUserCommandHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IUserBlockService userBlockService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _userBlockService = userBlockService;
        }

        public async Task<UnblockUserResponse> Handle(UnblockUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = await _currentUserService.GetUserAsync();
                var currentUserProfile = await _dbContext.Profiles
                    .FirstOrDefaultAsync(p => p.UserId == currentUser.Id, cancellationToken);

                if (currentUserProfile == null)
                {
                    return new UnblockUserResponse
                    {
                        Success = false,
                        Message = "Current user profile not found."
                    };
                }

                // Find the block
                var block = await _dbContext.UserBlocks
                    .FirstOrDefaultAsync(b => 
                        b.BlockerProfileId == currentUserProfile.Uid && 
                        b.BlockedProfileId == request.ProfileIdToUnblock &&
                        b.IsActive,
                        cancellationToken);

                if (block == null)
                {
                    return new UnblockUserResponse
                    {
                        Success = false,
                        Message = "User is not blocked."
                    };
                }

                // Set block as inactive instead of removing it
                //block.IsActive = false;
                //block.UpdatedAt = DateTime.UtcNow;
              
                _dbContext.UserBlocks.Remove(block);

                // Restore all interactions between the users
                await _userBlockService.HandleUserUnblock(
                    currentUserProfile.Uid,
                    request.ProfileIdToUnblock,
                    cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                return new UnblockUserResponse
                {
                    Success = true,
                    Message = "User unblocked successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking user");
                return new UnblockUserResponse
                {
                    Success = false,
                    Message = "An error occurred while unblocking the user."
                };
            }
        }
    }
} 