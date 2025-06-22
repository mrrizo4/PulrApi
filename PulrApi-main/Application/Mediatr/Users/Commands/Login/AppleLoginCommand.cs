using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Users;
using Core.Application.Models.External.Apple;

namespace Core.Application.Mediatr.Users.Commands.Login
{
    public class AppleLoginCommand : IRequest<LoginResponse>
    {
        [Required]
        public string IdentityToken { get; set; }

        public AppleNameInfo FullName { get; set; }
    }

    public class AppleLoginCommandHandler : IRequestHandler<AppleLoginCommand, LoginResponse>
    {
        private readonly IUserService _userService;

        public AppleLoginCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<LoginResponse> Handle(AppleLoginCommand request, CancellationToken cancellationToken)
        {
            return await _userService.LoginWithAppleAsync(request.IdentityToken, request.FullName);
        }
    }
}
