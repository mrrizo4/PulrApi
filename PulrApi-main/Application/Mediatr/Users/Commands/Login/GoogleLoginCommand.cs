using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Users.Commands.Login;
using Core.Application.Models.Users;

namespace Core.Application.Mediatr.Users.Commands.Login
{
    public class GoogleLoginCommand : IRequest<LoginResponse>
    {
        [Required]
        public string AccessToken { get; set; }
        
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PictureUrl { get; set; }
        public bool IsEmailVerified { get; set; }
    }

    public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, LoginResponse>
    {
        private readonly IUserService _userService;

        public GoogleLoginCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<LoginResponse> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
        {
            return await _userService.LoginWithGoogleAsync(
                request.AccessToken,
                request.FirstName,
                request.LastName,
                request.PictureUrl,
                request.IsEmailVerified
            );
        }
    }
} 