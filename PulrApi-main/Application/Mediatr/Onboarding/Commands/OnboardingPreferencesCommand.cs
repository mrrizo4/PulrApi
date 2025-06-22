using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Onboarding.Commands
{
    public class OnboardingPreferencesCommand : IRequest <Unit>
    {
        public string[] Preferences { get; set; }
    }

    public class OnboardingPreferencesCommandHandler : IRequestHandler<OnboardingPreferencesCommand,Unit>
    {
        private readonly ILogger<OnboardingPreferencesCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public OnboardingPreferencesCommandHandler(ILogger<OnboardingPreferencesCommandHandler> logger, IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(OnboardingPreferencesCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = await _currentUserService.GetUserAsync();
                var existing = _dbContext.ProfileOnboardingPreferences.Where(p => p.ProfileId == currentUser.Profile.Id).ToList();
                if (existing.Any())
                {
                    _dbContext.ProfileOnboardingPreferences.RemoveRange(existing);
                }

                var preferencesToAdd = await _dbContext.OnboardingPreferences.Where(e => request.Preferences.Contains(e.Key))
                                                                             .Select(e =>
                                                                                new ProfileOnboardingPreference()
                                                                                {
                                                                                    OnboardingPreferenceId = e.Id,
                                                                                    ProfileId = currentUser.Profile.Id
                                                                                }).ToListAsync();

                _dbContext.ProfileOnboardingPreferences.AddRange(preferencesToAdd);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
        }
    }
}
