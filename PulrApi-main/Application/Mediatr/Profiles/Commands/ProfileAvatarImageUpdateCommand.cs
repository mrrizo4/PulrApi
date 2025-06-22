using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Profiles.Commands;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Profiles.Commands
{
    public class ProfileAvatarImageUpdateCommand : IRequest<string>
    {
        [Required]
        [MaxFileSize(5 * 1024 * 1024)]
        [PulrFileValidation(new FileTypeEnum[] { FileTypeEnum.Image })]
        public IFormFile Image { get; set; }
    }

    public class ProfileAvatarImageUpdateCommandHandler : IRequestHandler<ProfileAvatarImageUpdateCommand, string>
    {
        private readonly ILogger<ProfileAvatarImageUpdateCommandHandler> _logger;
        private readonly IProfileService _profileService;
        private readonly ICurrentUserService _currentUserService;

        public ProfileAvatarImageUpdateCommandHandler(ILogger<ProfileAvatarImageUpdateCommandHandler> logger, IProfileService profileService, ICurrentUserService currentUserService)
        {
            _logger = logger;
            _profileService = profileService;
            _currentUserService = currentUserService;
        }
        public async Task<string> Handle(ProfileAvatarImageUpdateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync();
                var imagePath = await _profileService.ProfileUpdateAvatarImage(user.Profile, request.Image);
                return imagePath;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }


}
