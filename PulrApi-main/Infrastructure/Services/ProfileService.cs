using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Currencies;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using Core.Domain.Enums;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Profile = Core.Domain.Entities.Profile;

namespace Core.Infrastructure.Services
{
    public class ProfileService : IProfileService
    {
        private readonly ILogger<ProfileService> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IQueryHelperService _queryHelperService;
        private readonly IFileUploadService _fileUploadService;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public ProfileService(ILogger<ProfileService> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IQueryHelperService queryHelperService,
            IFileUploadService fileCloudService,
            UserManager<User> userManager,
            IConfiguration configuration,
            IMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _queryHelperService = queryHelperService;
            _fileUploadService = fileCloudService;
            _userManager = userManager;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task Create(User user, GenderEnum? gender = null)
        {
            try
            {
                bool alreadyExists = await _dbContext.Profiles.AnyAsync(p => p.UserId == user.Id);
                if (alreadyExists)
                {
                    throw new ForbiddenException("Profile already exists");
                }

                var genderEntity = gender == null
                    ? await _dbContext.Genders.SingleOrDefaultAsync(g => g.Key == GenderEnum.Hidden.ToString())
                    : await _dbContext.Genders.SingleOrDefaultAsync(g => g.Key == gender.ToString());

                var profile = new Profile()
                {
                    User = user,
                    Gender = genderEntity,
                    Currency = await _dbContext.Currencies.SingleOrDefaultAsync(c =>
                        c.Code == _configuration["ProfileSettings:DefaultCurrencyCode"]),
                    ProfileSettings = new ProfileSettings
                    {
                        IsProfilePublic = true,
                        ShowSocialMediaLinks = true,
                        ShowFollowers = true,
                        ShowFollowing = true,
                        ShowLocation = true,
                        ShowAbout = true
                    }
                };

                _dbContext.Profiles.Add(profile);
                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<MyProfileDetailsResponse> GetMy()
        {
            try
            {
                var user = await _currentUserService.GetUserAsync();

                if (user == null || user.Profile == null)
                {
                    throw new ForbiddenException();
                }

                var profileMapped = _mapper.Map<MyProfileDetailsResponse>(user.Profile);
                profileMapped.FullName = user.FirstName;
                profileMapped.FirstName = user.FirstName;
                profileMapped.LastName = user.LastName;
                profileMapped.Username = user.UserName;
                profileMapped.Email = user.Email;
                profileMapped.PhoneNumber = user.PhoneNumber;

                // Location information is always visible
                profileMapped.Address = user.Address;
                profileMapped.ZipCode = user.ZipCode;
                profileMapped.CityName = user.CityName;
                profileMapped.Gender = user.Profile.Gender?.Key;
                profileMapped.Location = user.Profile.Location;

                // Social media links are always visible
                profileMapped.WebsiteUrl = user.Profile.ProfileSocialMedia?.WebsiteUrl;
                profileMapped.InstagramUrl = user.Profile.ProfileSocialMedia?.InstagramUrl;
                profileMapped.FacebookUrl = user.Profile.ProfileSocialMedia?.FacebookUrl;
                profileMapped.TwitterUrl = user.Profile.ProfileSocialMedia?.TwitterUrl;
                profileMapped.TikTokUrl = user.Profile.ProfileSocialMedia?.TikTokUrl;
                profileMapped.SocialMediaLinks = user.Profile.ProfileSocialMediaLinks
                    .Select(l => new ProfileSocialMediaLinkDto
                    {
                        Url = l.Url,
                        Title = l.Title,
                        Type = l.Type,
                    }).ToList();
                // profileMapped.Followers = await _dbContext.ProfileFollowers
                //     .Where(e => e.ProfileId == user.Profile.Id)
                //     .Select(f => f.Follower.Uid).ToListAsync();

                // About section is always visible
                profileMapped.About = user.Profile.About;

                // Followers and following counts are always visible
                profileMapped.Followers = await _dbContext.ProfileFollowers
                    .CountAsync(e => e.ProfileId == user.Profile.Id);
                profileMapped.Following = await _dbContext.ProfileFollowers
                    .CountAsync(e => e.FollowerId == user.Profile.Id);

                // Posts count is only visible if profile is public or user is following
                if (user.Profile.ProfileSettings.IsProfilePublic)
                {
                    profileMapped.PostsCount = await _dbContext.Posts
                        .CountAsync(p => p.User.Id == user.Id);
                }
                else
                {
                    // Check if the current user is following this profile
                    var isFollowing = await _dbContext.ProfileFollowers
                        .AnyAsync(pf => pf.ProfileId == user.Profile.Id && 
                                      pf.Follower.UserId == _currentUserService.GetUserId());
                    
                    if (isFollowing)
                    {
                        profileMapped.PostsCount = await _dbContext.Posts
                            .CountAsync(p => p.User.Id == user.Id);
                    }
                    else
                    {
                        profileMapped.PostsCount = 0; // Hide posts count for non-followers
                    }
                }

                if (user.Country != null)
                {
                    profileMapped.CountryUid = user.Country.Uid;
                }

                if (user.Profile.CurrencyId != null)
                {
                    profileMapped.Currency = _mapper.Map<CurrencyDetailsResponse>(
                        await _dbContext.Currencies.SingleOrDefaultAsync(c => c.Id == user.Profile.CurrencyId));
                    if (profileMapped.Currency != null)
                    {
                        profileMapped.CurrencyUid = profileMapped.Currency.Uid;
                    }
                }

                // profileMapped.Following = await _dbContext.ProfileFollowers
                //     .Where(e => e.FollowerId == user.Profile.Id)
                //     .Select(f => f.Profile.Uid).ToListAsync();

                profileMapped.StoreUids = await _dbContext.Stores.Where(s => s.UserId == user.Id).Select(s => s.Uid).ToListAsync();

                profileMapped.Stores = await _dbContext.Stores.Where(s => s.UserId == user.Id)
                    .Select(s => new StoreDetailsResponse()
                    {
                        Uid = s.Uid,
                        UniqueName = s.UniqueName,
                        Name = s.Name,
                        ImageUrl = s.ImageUrl
                    }).ToListAsync();

                return profileMapped;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<(string username, string uid)> ProfileToggleFollow(string profileUid)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                if (cUser.Profile == null)
                {
                    throw new ForbiddenException($"Profile not found for user '{cUser.Id}'.");
                }

                var profile = await _dbContext.Profiles.Include(p => p.User).SingleOrDefaultAsync(p => p.IsActive && p.Uid == profileUid);
                if (profile == null)
                {
                    throw new BadRequestException($"Profile with uid '{profileUid}' doesn't exist.");
                }

                var follower =
                    await _dbContext.Profiles.SingleOrDefaultAsync(p => p.IsActive && p.Uid == cUser.Profile.Uid);
                if (follower == null)
                {
                    throw new BadRequestException($"Follower with uid '{cUser.Profile.Uid}' doesn't exist.");
                }

                // Check if either user has blocked the other
                var isBlocked = await _dbContext.UserBlocks
                    .AnyAsync(ub => 
                        (ub.BlockerProfileId == profileUid && ub.BlockedProfileId == cUser.Profile.Uid) ||
                        (ub.BlockerProfileId == cUser.Profile.Uid && ub.BlockedProfileId == profileUid) &&
                        ub.IsActive);

                if (isBlocked)
                {
                    throw new BadRequestException("Cannot follow this user as one of you has blocked the other.");
                }

                var pfm = await _dbContext.ProfileFollowers
                    .Where(pfm => pfm.Profile.Uid == profileUid && pfm.Follower.Uid == cUser.Profile.Uid)
                    .SingleOrDefaultAsync();
                if (pfm != null)
                {
                    _dbContext.ProfileFollowers.Remove(pfm);
                }
                else
                {
                    _dbContext.ProfileFollowers.Add(new ProfileFollower() { Profile = profile, Follower = follower });
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);

                return (profile.User.UserName, profile.Uid);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<string> ProfileUpdateAvatarImage(Profile profile, IFormFile image)
        {
            try
            {
                if (profile == null)
                {
                    throw new ForbiddenException("Profile cannot be null.");
                }

                string bucketName = _configuration[AwsLocationNames.S3UploadBucket];
                string folderPath = _configuration[AwsLocationNames.PublicUploadFolder];

                var fileConfig = new FileUploadConfigDto()
                {
                    FileName = image.FileName,
                    BucketName = bucketName,
                    FolderPath = folderPath,
                    File = image,
                    ImageWidth = PulrGlobalConfig.AvatarImage.Width,
                    ImageHeight = PulrGlobalConfig.AvatarImage.Height,
                };

                if (profile.ImageUrl != null)
                {
                    fileConfig.OldFileName = profile.ImageUrl.Substring(profile.ImageUrl.LastIndexOf("/") + 1);
                    await _fileUploadService.Delete(fileConfig);
                }

                string path = await _fileUploadService.UploadImage(fileConfig);
                profile.ImageUrl = path;

                await _dbContext.SaveChangesAsync(CancellationToken.None);

                return path;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<List<string>> SearchHandles(string search)
        {
            try
            {
                var handles = new List<string>();

                var profileHandles = await _dbContext.Profiles
                    .Where(p => p.User.UserName.StartsWith(search) && p.IsActive).Take(10).Select(p => p.User.UserName)
                    .ToListAsync();
                handles.AddRange(profileHandles);
                var storeHandles = await _dbContext.Stores.Where(s => s.UniqueName.StartsWith(search) && s.IsActive)
                    .Take(10).Select(s => s.UniqueName).ToListAsync();
                handles.AddRange(storeHandles);
                return handles;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task Update(ProfileUpdateDto model)
        {
            try
            {
                var username = UsernameHelper.Normalize(model.Username);
                var user = await _currentUserService.GetUserAsync();
                var changes = new List<string>();
                var warnings = new List<string>();

                if (user?.Profile == null)
                {
                    throw new BadRequestException("Profile not found.");
                }

                if (!String.IsNullOrWhiteSpace(model.FirstName) && model.FirstName != user.FirstName)
                {
                    user.FirstName = model.FirstName;
                    changes.Add("First name");
                }

                if (!String.IsNullOrWhiteSpace(model.LastName) && model.LastName != user.LastName)
                {
                    user.LastName = model.LastName;
                    changes.Add("Last name");
                }

                if (!String.IsNullOrWhiteSpace(model.DisplayName) && model.DisplayName != user.DisplayName)
                {
                    try
                    {
                        CheckIfDisplayNameChangeIsAllowed(user);
                        user.DisplayName = model.DisplayName;
                        changes.Add("Display name");
                    }
                    catch (ValidationException)
                    {
                        warnings.Add("Display name can't be changed for 30 days");
                    }
                }

                if (!String.IsNullOrWhiteSpace(username) && username != user.UserName)
                {
                    try
                    {
                        CheckIfUsernameChangeIsAllowed(user);

                        bool usernameTaken =
                            await _dbContext.Users.AnyAsync(u => u.UserName == username && u.Id != user.Id);
                        if (usernameTaken)
                        {
                            throw new ForbiddenException("Username taken.");
                        }

                        user.UserName = username;
                        changes.Add("Username");
                    }
                    catch (ValidationException)
                    {
                        warnings.Add("Username can't be changed for 30 days");
                    }
                }

                user.Address = model.Address ?? user.Address;
                user.ZipCode = model.ZipCode ?? user.ZipCode;
                user.CityName = model.CityName ?? user.CityName;
                user.Country = await _dbContext.Countries.SingleOrDefaultAsync(c => c.Uid == model.CountryUid);

                user.Profile.ProfileSocialMedia.WebsiteUrl = model.WebsiteUrl;
                user.Profile.ProfileSocialMedia.FacebookUrl = model.FacebookUrl;
                user.Profile.ProfileSocialMedia.TikTokUrl = model.TikTokUrl;
                user.Profile.ProfileSocialMedia.InstagramUrl = model.InstagramUrl;
                user.Profile.ProfileSocialMedia.TwitterUrl = model.TwitterUrl;

                // TODO ask for Email update, should we do it ???
                user.Profile.About = model.About ?? user.Profile.About;
                if (!String.IsNullOrWhiteSpace(model.CurrencyUid))
                {
                    user.Profile.Currency =
                        await _dbContext.Currencies.SingleOrDefaultAsync(c => c.Uid == model.CurrencyUid);
                }
                else if (user.Profile.Currency == null)
                {
                    user.Profile.Currency = await _dbContext.Currencies.SingleOrDefaultAsync(c =>
                        c.Code == _configuration["ProfileSettings:DefaultCurrencyCode"]);
                }

                var genderEntity = model.Gender == null
                    ? await _dbContext.Genders.SingleOrDefaultAsync(g => g.Key == GenderEnum.Hidden.ToString())
                    : await _dbContext.Genders.SingleOrDefaultAsync(g => g.Key == model.Gender.ToString());

                //if(_userManager.phone)
                await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);
                user.Profile.Gender = genderEntity;

                await _dbContext.SaveChangesAsync(CancellationToken.None);

                if (changes.Any())
                {
                    var message = $"Changes updated: {string.Join(", ", changes)}";
                    if (warnings.Any())
                    {
                        message += $". {string.Join(". ", warnings)}";
                    }
                    throw new SuccessException(message);
                }
                else if (warnings.Any())
                {
                    throw new SuccessException($"No changes were made. {string.Join(". ", warnings)}");
                }
                else
                {
                    throw new SuccessException("No changes were made");
                }
            }
            catch (SuccessException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<List<ProfileResponse>> MapProfileResponseList(IQueryable<Profile> profiles, CancellationToken ct)
        {
            var profilesResponse =  await profiles.Select(p => new ProfileResponse
            {
                FullName = p.User.FirstName,
                FirstName = p.User.FirstName,
                LastName = p.User.LastName,
                Followers = p.ProfileFollowers.Count(),
                Following = p.ProfileFollowings.Count(),
                ImageUrl = p.ImageUrl,
                UserId = p.UserId,
                Uid = p.Uid,
                Username = p.User.UserName
            }).ToListAsync(ct);
            foreach (var item in profilesResponse)
            {
                item.IsInfluencer = await _userManager.IsInRoleAsync(new User { Id = item.UserId }, PulrRoles.Influencer);
            }

            return profilesResponse;
        }

        private void CheckIfUsernameChangeIsAllowed(User user)
        {
            if (user.UsernameChangeDate > DateTime.UtcNow.AddDays(-14) && user.UsernameChangesCount >= 2)
            {
                var daysUntilAvailable =
                    Convert.ToInt32(14 - (DateTime.UtcNow.Subtract(user.UsernameChangeDate).TotalDays));
                var vf = new ValidationFailure(nameof(user.UserName),
                    "Handle can't be changed in next " + daysUntilAvailable + " days.");
                throw new ValidationException(new List<ValidationFailure>() { vf });
            }
            else if (user.UsernameChangeDate < DateTime.UtcNow.AddDays(-14) && user.UsernameChangesCount >= 2)
            {
                user.UsernameChangesCount = 0;
            }

            if (user.UsernameChangesCount < 2)
            {
                if (user.UsernameChangesCount == 0)
                {
                    user.UsernameChangeDate = DateTime.UtcNow;
                }

                user.UsernameChangesCount += 1;
            }
        }

        private void CheckIfDisplayNameChangeIsAllowed(User user)
        {
            if (user.DisplayNameChangeDate > DateTime.UtcNow.AddDays(-30) && user.DisplayNameChangesCount >= 1)
            {
                var daysUntilAvailable =
                    Convert.ToInt32(30 - (DateTime.UtcNow.Subtract(user.DisplayNameChangeDate).TotalDays));
                var vf = new ValidationFailure(nameof(user.DisplayName),
                    "Display name can't be changed in next " + daysUntilAvailable + " days.");
                throw new ValidationException(new List<ValidationFailure>() { vf });
            }
            else if (user.DisplayNameChangeDate < DateTime.UtcNow.AddDays(-30) && user.DisplayNameChangesCount >= 1)
            {
                user.DisplayNameChangesCount = 0;
            }

            if (user.DisplayNameChangesCount < 1)
            {
                if (user.DisplayNameChangesCount == 0)
                {
                    user.DisplayNameChangeDate = DateTime.UtcNow;
                }

                user.DisplayNameChangesCount += 1;
            }
        }
    }
}