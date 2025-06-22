using MediatR;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;

namespace Core.Application.Mediatr.Users.Commands
{
    public class AcceptTermsCommand : IRequest<bool>
    {
        [Required]
        public string Email { get; set; }
    }

    public class AcceptTermsCommandHandler : IRequestHandler<AcceptTermsCommand, bool>
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AcceptTermsCommandHandler> _logger;

        public AcceptTermsCommandHandler(UserManager<User> userManager, ILogger<AcceptTermsCommandHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<bool> Handle(AcceptTermsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning($"Terms acceptance failed: User with email {request.Email} not found");
                    throw new BadRequestException("User not found");
                }

                user.TermsAccepted = true;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning($"Failed to update terms acceptance for user {user.Email}: {errors}");
                    throw new BadRequestException("Failed to update terms acceptance");
                }

                _logger.LogInformation($"Terms accepted successfully for user {user.Email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error accepting terms for {request.Email}");
                throw;
            }
        }
    }
} 