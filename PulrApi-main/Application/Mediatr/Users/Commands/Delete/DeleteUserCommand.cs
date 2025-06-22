using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Users.Commands.Delete;
using Core.Application.Mediatr.Users.Commands.Register;

namespace Core.Application.Mediatr.Users.Commands.Delete
{
    public class DeleteUserCommand : IRequest <Unit> { }

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand,Unit>
    {
        private readonly ILogger<RegisterCommandHandler> logger;
        private readonly ICurrentUserService currentUserService;
        private readonly IUserService userService;

        public DeleteUserCommandHandler(ILogger<RegisterCommandHandler> logger,
            ICurrentUserService currentUserService,
            IUserService userService)
        {
            this.logger = logger;
            this.currentUserService = currentUserService;
            this.userService = userService;
        }

        public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = await currentUserService.GetUserAsync(true);
                if (currentUserService.HasRole(PulrRoles.Administrator))
                {
                    throw new ForbiddenException("Delete yourself? Are u drunk?");
                }
                await userService.DeactivateAsync(currentUser);

                return Unit.Value;
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
