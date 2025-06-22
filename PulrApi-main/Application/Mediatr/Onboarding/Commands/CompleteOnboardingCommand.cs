using Core.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Onboarding.Commands
{
    public class CompleteOnboardingCommand : IRequest <Unit>
    {
    }

    public class CompleteOnboardingCommandHandler : IRequestHandler<CompleteOnboardingCommand,Unit>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public CompleteOnboardingCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = await _currentUserService.GetUserAsync();
                // Check if user has any preferences set
                var hasPreferences = await  _dbContext.ProfileOnboardingPreferences
                    .AnyAsync(p => p.ProfileId == currentUser.Profile.Id,cancellationToken);
                if (!hasPreferences)
                {
                    throw new ValidationException("Please set your preferences before completing onboarding");
                }

                //mark the onboarding as completed
                //if(!currentUser.IsVerified)
                //{
                //    currentUser.IsVerified = true;
                //    _dbContext.Users.Update(currentUser);
                //    await _dbContext.SaveChangesAsync(cancellationToken);
                //}
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Unit.Value;
            }
            catch (Exception e)
            {
                //log the error
                throw new Exception("An error occurred while completing onboarding", e);
            }
        }
    }
}
