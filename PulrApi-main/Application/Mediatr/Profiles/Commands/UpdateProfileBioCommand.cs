using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Models.Profiles;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Exceptions;
using FluentValidation.Results;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Core.Application.Mediatr.Profiles.Commands;

public class UpdateProfileBioCommand : IRequest<ProfileBioDto>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DisplayName { get; set; }
    public string Username { get; set; }
    public string About { get; set; }
    public string Location { get; set; }
    public string PhoneNumber { get; set; }
    public string WebsiteUrl { get; set; }
    public string InstagramUrl { get; set; }
    public string FacebookUrl { get; set; }
    public string TwitterUrl { get; set; }
    public string TikTokUrl { get; set; }
    public List<ProfileSocialMediaLinkDto> SocialMediaLinks { get; set; }
}

public class UpdateProfileBioCommandHandler : IRequestHandler<UpdateProfileBioCommand, ProfileBioDto>
{
    private readonly IMapper _mapper;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateProfileBioCommandHandler> _logger;
    private readonly UserManager<User> _userManager;

    public UpdateProfileBioCommandHandler(
        IMapper mapper,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<UpdateProfileBioCommandHandler> logger,
        UserManager<User> userManager)
    {
        _mapper = mapper;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
        _userManager = userManager;
    }

    public async Task<ProfileBioDto> Handle(UpdateProfileBioCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();
            var changes = new List<string>();
            var warnings = new List<string>();

            var user = await _dbContext.Users
                .Where(u => u.Id == currentUser.Id)
                .Include(u => u.Profile)
                    .ThenInclude(p => p.ProfileSocialMedia)
                .Include(u => u.Profile)
                    .ThenInclude(p => p.ProfileSocialMediaLinks)
                .SingleOrDefaultAsync(cancellationToken);

            if (user == null || user.Profile == null)
            {
                throw new NotFoundException("User or profile not found");
            }

            // Update basic profile info only if provided
            if (!string.IsNullOrEmpty(request.FirstName) && request.FirstName != user.FirstName)
            {
                user.FirstName = request.FirstName;
                changes.Add("First name");
            }

            if (!string.IsNullOrEmpty(request.LastName) && request.LastName != user.LastName)
            {
                user.LastName = request.LastName;
                changes.Add("Last name");
            }

            // Handle display name change
            if (!string.IsNullOrEmpty(request.DisplayName) && request.DisplayName != user.DisplayName)
            {
                try
                {
                    CheckIfDisplayNameChangeIsAllowed(user);
                    user.DisplayName = request.DisplayName;
                    changes.Add("Display name");
                }
                catch (ValidationException)
                {
                    warnings.Add("Display name can't be changed for 30 days");
                }
            }

            // Handle username change
            if (!string.IsNullOrEmpty(request.Username) && request.Username != user.UserName)
            {
                try
                {
                    CheckIfUsernameChangeIsAllowed(user);
                    user.UserName = request.Username;
                    changes.Add("Username");
                }
                catch (ValidationException)
                {
                    warnings.Add("Username can't be changed for 30 days");
                }
            }

            // Update phone number if provided
            if (!string.IsNullOrEmpty(request.PhoneNumber) && request.PhoneNumber != user.PhoneNumber)
            {
                await _userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
                changes.Add("Phone number");
            }

            // Update other profile info
            if (!string.IsNullOrEmpty(request.About))
            {
                user.Profile.About = request.About;
                changes.Add("About");
            }
            if (!string.IsNullOrEmpty(request.Location))
            {
                user.Profile.Location = request.Location;
                changes.Add("Location");
            }

            // Update social media
            user.Profile.ProfileSocialMedia = await _dbContext.ProfileSocialMedias.SingleOrDefaultAsync(psm => psm.ProfileId == user.Profile.Id) ?? new ProfileSocialMedia();

            user.Profile.ProfileSocialMedia.WebsiteUrl = string.IsNullOrEmpty(request.WebsiteUrl) ? user.Profile.ProfileSocialMedia.WebsiteUrl : request.WebsiteUrl;
            user.Profile.ProfileSocialMedia.FacebookUrl = string.IsNullOrEmpty(request.FacebookUrl) ? user.Profile.ProfileSocialMedia.FacebookUrl : request.FacebookUrl;
            user.Profile.ProfileSocialMedia.InstagramUrl = string.IsNullOrEmpty(request.InstagramUrl) ? user.Profile.ProfileSocialMedia.InstagramUrl : request.InstagramUrl;
            user.Profile.ProfileSocialMedia.TwitterUrl = string.IsNullOrEmpty(request.TwitterUrl) ? user.Profile.ProfileSocialMedia.TwitterUrl : request.TwitterUrl;
            user.Profile.ProfileSocialMedia.TikTokUrl = string.IsNullOrEmpty(request.TikTokUrl) ? user.Profile.ProfileSocialMedia.TikTokUrl : request.TikTokUrl;

            // Update social media links
            if (request.SocialMediaLinks != null && request.SocialMediaLinks.Any())
            {
                foreach (var link in request.SocialMediaLinks)
                {
                    // Find existing link with same type
                    var existingLink = user.Profile.ProfileSocialMediaLinks
                        .FirstOrDefault(l => l.Type == link.Type);

                    if (existingLink != null)
                    {
                        // Update existing link
                        existingLink.Url = link.Url;
                        existingLink.Title = link.Title;
                        changes.Add($"{link.Type} social media link");
                    }
                    else
                    {
                        // Create new link
                        user.Profile.ProfileSocialMediaLinks.Add(new ProfileSocialMediaLink
                        {
                            Url = link.Url,
                            Title = link.Title,
                            Type = link.Type,
                            ProfileId = user.Profile.Id
                        });
                        changes.Add($"New {link.Type} social media link");
                    }
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Reload the user with all necessary includes
            user = await _dbContext.Users
                .Where(u => u.Id == currentUser.Id)
                .Include(u => u.Profile)
                .Include(u => u.Profile.ProfileSocialMedia)
                .Include(u => u.Profile.ProfileSocialMediaLinks)
                .SingleOrDefaultAsync(cancellationToken);

            var response = _mapper.Map<ProfileBioDto>(user.Profile);
            response.FullName = user.FirstName;
            response.FirstName = user.FirstName;
            response.LastName = user.LastName;
            response.Username = user.UserName;
            response.DisplayName = user.DisplayName;
            response.WebsiteUrl = user.Profile.ProfileSocialMedia.WebsiteUrl;
            response.PhoneNumber = user.PhoneNumber;

            if (changes.Any())
            {
                var message = $"Changes updated: {string.Join(", ", changes)}";
                if (warnings.Any())
                {
                    message += $". {string.Join(". ", warnings)}";
                }
                response.Message = message;
            }
            else if (warnings.Any())
            {
                response.Message = $"No changes were made. {string.Join(". ", warnings)}";
            }

            return response;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating short bio for profile");
            throw;
        }
    }

    private void CheckIfUsernameChangeIsAllowed(User user)
    {
        if (user.UsernameChangeDate > DateTime.UtcNow.AddDays(-30) && user.UsernameChangesCount >= 1)
        {
            var daysUntilAvailable = Convert.ToInt32(30 - (DateTime.UtcNow.Subtract(user.UsernameChangeDate).TotalDays));
            var vf = new ValidationFailure(nameof(user.UserName),
                "Username can't be changed in next " + daysUntilAvailable + " days.");
            throw new ValidationException(new List<ValidationFailure>() { vf });
        }
        else if (user.UsernameChangeDate < DateTime.UtcNow.AddDays(-30) && user.UsernameChangesCount >= 1)
        {
            user.UsernameChangesCount = 0;
        }

        if (user.UsernameChangesCount < 1)
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
            var daysUntilAvailable = Convert.ToInt32(30 - (DateTime.UtcNow.Subtract(user.DisplayNameChangeDate).TotalDays));
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