using System.Threading;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Users.Commands.Deactivate
{
    public class DeactivateUserCommand : IRequest<Unit>
    {
        public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Unit>
        {
            private readonly ILogger<DeactivateUserCommandHandler> _logger;
            private readonly ICurrentUserService _currentUserService;
            private readonly UserManager<User> _userManager;
            private readonly IUserService _userService;

            public DeactivateUserCommandHandler(
                ILogger<DeactivateUserCommandHandler> logger,
                ICurrentUserService currentUserService,
                UserManager<User> userManager,
                IUserService userService)
            {
                _logger = logger;
                _currentUserService = currentUserService;
                _userManager = userManager;
                _userService = userService;
            }

            public async Task<Unit> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
            {
                var currentUser = await _userManager.FindByIdAsync(_currentUserService.GetUserId());
                if (currentUser == null)
                {
                    throw new NotFoundException("User not found.");
                }

                await _userService.DeactivateAccountAsync(currentUser);
                return Unit.Value;
            }
        }
    }
} 