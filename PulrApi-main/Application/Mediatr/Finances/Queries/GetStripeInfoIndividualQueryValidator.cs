using Core.Application.Constants;
using Core.Application.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Finances.Queries
{
    public class GetStripeInfoIndividualQueryValidator : AbstractValidator<GetStripeInfoIndividualQuery>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public GetStripeInfoIndividualQueryValidator(ICurrentUserService currentUserService, IApplicationDbContext dbContext)
        {
            _currentUserService = currentUserService;
            _dbContext = dbContext;

            RuleFor(e => e.Username)
                .MustAsync(ValidUsernameForUserType).WithMessage("Username not valid.");
        }

        public async Task<bool> ValidUsernameForUserType(string username, CancellationToken ct)
        {
            if (_currentUserService.HasRole(PulrRoles.Administrator))
            {
                bool userExists = await _dbContext.Users.Where(e => e.UserName == username &&
                                         e.Profile.IsActive &&
                                         e.Profile.StripeConnectedAccount != null).AnyAsync();

                if (string.IsNullOrWhiteSpace(username) || !userExists)
                {
                    return false;
                }

                return true;
            }

            var currentUser = await _currentUserService.GetUserAsync();

            return await _dbContext.Users.Where(e => e.UserName == currentUser.UserName && 
                                                     e.Profile.IsActive && 
                                                     e.Profile.StripeConnectedAccount != null).AnyAsync();
        }

    }
}