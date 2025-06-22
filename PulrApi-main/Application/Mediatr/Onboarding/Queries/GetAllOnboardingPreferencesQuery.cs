using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Models.Onboarding;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Onboarding.Queries
{
    public class GetAllOnboardingPreferencesQuery : IRequest<OnboardingPreferencesResponse>
    {
    }

    public class GetAllOnboardingPreferencesQueryHandler : IRequestHandler<GetAllOnboardingPreferencesQuery, OnboardingPreferencesResponse>
    {
        private readonly ILogger<GetAllOnboardingPreferencesQueryHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IApplicationDbContext _dbContext;

        public GetAllOnboardingPreferencesQueryHandler(ILogger<GetAllOnboardingPreferencesQueryHandler> logger, IMapper mapper, IApplicationDbContext dbContext)
        {
            _logger = logger;
            _mapper = mapper;
            _dbContext = dbContext;
        }

        public async Task<OnboardingPreferencesResponse> Handle(GetAllOnboardingPreferencesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var preferences = await _dbContext.OnboardingPreferences.Select(e => new OnboardingPreferenceResponse()
                                                                        {
                                                                           Name = e.Name,
                                                                           Key = e.Key,
                                                                           Description = e.Description,
                                                                           Gender = e.Gender.Key,
                                                                        }).ToListAsync(cancellationToken);

                return new OnboardingPreferencesResponse()
                {
                    Preferences = preferences
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
