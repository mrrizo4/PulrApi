using Core.Application.Constants;
using Core.Application.Interfaces;
using Core.Application.Models.Onboarding;
using Core.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Onboarding.Queries
{
    public class GetOnboardingWhoToFollowQuery : IRequest<OnboardingWhoToFollowResponse>
    {
        public bool IsRefetch { get; set; }
        public bool FetchAnotherProfile { get; set; }
        public bool FetchAnotherInfluencer { get; set; }
        public List<string> ProfilesToSkip { get; set; } = new List<string>();
        public bool FetchAnotherStore { get; set; }
        public List<string> StoresToSkip { get; set; } = new List<string>();
    }

    public class GetOnboardingWhoToFollowQueryHandler : IRequestHandler<GetOnboardingWhoToFollowQuery, OnboardingWhoToFollowResponse>
    {
        private readonly ILogger<GetOnboardingWhoToFollowQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public GetOnboardingWhoToFollowQueryHandler(ILogger<GetOnboardingWhoToFollowQueryHandler> logger, ICurrentUserService currentUserService, IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<OnboardingWhoToFollowResponse> Handle(GetOnboardingWhoToFollowQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = await _currentUserService.GetUserAsync();
                int? countryId = currentUser.Country != null ? currentUser.Country.Id : null;

                var existingRoles = await _currentUserService.GetRolesAsync();

                var userRole = existingRoles.Where(r => r.Name == PulrRoles.User).SingleOrDefault();
                var influencerRole = existingRoles.Where(r => r.Name == PulrRoles.Influencer).SingleOrDefault();

                var currentProfileFollowingUids = await _dbContext.ProfileFollowers.Where(e => e.Follower.Uid == currentUser.Profile.Uid)
                                                                                   .Select(u => u.Profile.Uid)
                                                                                   .ToListAsync();

                var regularUsers = new List<OnboardingProfileResponse>();
                if (!request.IsRefetch || (request.IsRefetch && request.FetchAnotherProfile))
                {
                    regularUsers = await FetchRegularUsers(currentUser, request, userRole, influencerRole, countryId, currentProfileFollowingUids);
                }

                var influencers = new List<OnboardingProfileResponse>();
                if (!request.IsRefetch || (request.IsRefetch && request.FetchAnotherInfluencer))
                {
                    influencers = await FetchInfluencers(currentUser, request, influencerRole, countryId, currentProfileFollowingUids);
                }

                var stores = new List<OnboardingStoreResponse>();
                if (!request.IsRefetch || (request.IsRefetch && request.FetchAnotherStore))
                {
                   stores = await FetchStores(currentUser, request, countryId);
                }

                return new OnboardingWhoToFollowResponse()
                {
                    Profiles = influencers.Concat(regularUsers).ToList(),
                    Stores = stores
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
        }

        private async Task<List<OnboardingProfileResponse>> FetchRegularUsers(User currentUser, GetOnboardingWhoToFollowQuery request, IdentityRole userRole, IdentityRole influencerRole, int? countryId, List<string> currentProfileFollowingUids)
        {

            IQueryable<User> usersQuery = _dbContext.Users.FromSqlInterpolated($"SELECT u.* FROM \"AspNetUsers\" u WHERE EXISTS (SELECT 1 FROM \"AspNetUserRoles\" ur WHERE u.\"Id\" = ur.\"UserId\" AND ur.\"RoleId\" = {userRole.Id}) AND NOT EXISTS (SELECT 1 FROM \"AspNetUserRoles\" ur  WHERE u.\"Id\" = ur.\"UserId\" AND ur.\"RoleId\" = {influencerRole.Id})");

            if (countryId != null)
            {
                // todo check
                usersQuery = usersQuery.Where(e => e.CountryId == countryId || true);
            }

            int numberOfUsersToFetch = request.FetchAnotherProfile ? 10 : 10;

            var profilesToSkip = currentProfileFollowingUids.Concat(request.ProfilesToSkip);

            return await usersQuery.Where(e => e.IsSuspended == false && e.Profile.IsActive && e.Id != currentUser.Id)
                                   .Where(e => profilesToSkip.Contains(e.Profile.Uid) == false)
                                   .Take(numberOfUsersToFetch)
                                   .OrderByDescending(e => e.Profile.ProfileFollowers.Count())
                                   .Select(e => new OnboardingProfileResponse()
                                   {
                                      About = e.Profile.About,
                                      IsInfluencer = false,
                                       FullName = e.FirstName,
                                       Name = e.FirstName + " " + e.LastName,
                                      Uid = e.Profile.Uid,
                                      Username = e.UserName,
                                      ImageUrl = e.Profile.ImageUrl,
                                      Followers = e.Profile.ProfileFollowers.Count()
                                   })
                                   .ToListAsync();
        }

        private async Task<List<OnboardingProfileResponse>> FetchInfluencers(User currentUser, GetOnboardingWhoToFollowQuery request, IdentityRole influencerRole, int? countryId, List<string> currentProfileFollowingUids)
        {
            int numberOfInfluencersToFetch = request.FetchAnotherInfluencer ? 8 : 10;

            IQueryable<User> influencersQuery = _dbContext.Users.FromSqlInterpolated($"SELECT u.* FROM \"AspNetUsers\" u WHERE EXISTS (SELECT 1 FROM \"AspNetUserRoles\" ur WHERE u.\"Id\" = ur.\"UserId\" AND ur.\"RoleId\" = {influencerRole.Id})");       

            if (countryId != null)
            {
                // todo check
                influencersQuery = influencersQuery.Where(e => e.CountryId == countryId || true);
            }

            var influencersToSkip = currentProfileFollowingUids.Concat(request.ProfilesToSkip);

            return await influencersQuery.Where(e => e.IsSuspended == false && e.Profile.IsActive && e.Id != currentUser.Id)
                                         .Where(e => influencersToSkip.Contains(e.Profile.Uid) == false)
                                         .Take(numberOfInfluencersToFetch)
                                         .OrderByDescending(e => e.Profile.ProfileFollowers.Count())
                                         .Select(e => new OnboardingProfileResponse()
                                         {
                                            About = e.Profile.About,
                                            IsInfluencer = true,
                                             FullName = e.FirstName,
                                             Name = e.FirstName + " " + e.LastName,
                                            Uid = e.Profile.Uid,
                                            Username = e.UserName,
                                            ImageUrl = e.Profile.ImageUrl,
                                            Followers = e.Profile.ProfileFollowers.Count()
                                         })
                                         .ToListAsync();
        }

        private async Task<List<OnboardingStoreResponse>> FetchStores(User currentUser, GetOnboardingWhoToFollowQuery request, int? countryId)
        {
            var currentUserFollowingStoreUids = await _dbContext.StoreFollowers.Where(e => e.Follower.Uid == currentUser.Profile.Uid).Select(u => u.Store.Uid).ToListAsync();

            IQueryable<Store> storesQuery = _dbContext.Stores;
            storesQuery = storesQuery.Where(e => e.IsActive && e.User.IsSuspended == false && e.User.Profile.IsActive);

            if (countryId != null)
            {
                storesQuery = storesQuery.Where(e => e.User.CountryId == countryId || true);
            }

            var storesToSkip = currentUserFollowingStoreUids.Concat(request.StoresToSkip);

            int numberOfStoresToFetch = request.FetchAnotherStore ? 5 : 6;

            return await storesQuery.Where(e => storesToSkip.Contains(e.Uid) == false)
                                    .Take(numberOfStoresToFetch)
                                    .OrderByDescending(e => e.StoreFollowers.Count())
                                    .Select(e => new OnboardingStoreResponse()
                                    {
                                        Uid = e.Uid,
                                        Name = e.Name,
                                        UniqueName = e.UniqueName,
                                        Description = e.Description,
                                        ImageUrl = e.ImageUrl
                                    })
                                    .ToListAsync();
        }
    }
}
