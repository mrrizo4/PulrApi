using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Users;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Users.Commands.Register
{
    public class RegisterCommand : IRequest<Unit>
    {
        [Required]
        [PulrNameValidation]
        public string FirstName { get; set; }

        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        [Required]
        [PulrUsernameValidation]
        public string Username { get; set; }

        [Required]
        [StrongPassword]
        public string Password { get; set; }

        public string CountryUid { get; set; }

        public GenderEnum? Gender { get; set; }

        [Required]
        public bool TermsAccepted { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Unit>
    {
        private readonly ILogger<RegisterCommandHandler> logger;
        private readonly IUserService userService;
        private readonly IProfileService profileService;

        public RegisterCommandHandler(ILogger<RegisterCommandHandler> logger,
            IUserService userService,
            IProfileService profileService)
        {
            this.logger = logger;
            this.userService = userService;
            this.profileService = profileService;
        }

        public async Task<Unit> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            if (!request.TermsAccepted)
            {
                throw new BadRequestException("You must accept the terms and conditions to register");
            }

            var registerDto = new UserRegisterDto
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Username = request.Username,
                Password = request.Password,
                CountryUid = request.CountryUid,
                Gender = request.Gender,
                TermsAccepted = request.TermsAccepted,
                DateOfBirth = request.DateOfBirth
            };

            var response = await userService.RegisterAsync(registerDto);
            if (!response.IsSuccess)
            {
                throw new BadRequestException(response.Message);
            }

            await profileService.Create(response.User, request.Gender);

            return Unit.Value;
        }
    }
}
