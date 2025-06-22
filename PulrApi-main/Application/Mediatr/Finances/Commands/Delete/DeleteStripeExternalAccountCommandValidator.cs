using Core.Application.Interfaces;
using FluentValidation;
using System.Threading.Tasks;
using System.Threading;
using Core.Application.Constants;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.Finances.Commands.Delete;

public class DeleteStripeExternalAccountCommandValidator : AbstractValidator<DeleteStripeExternalAccountCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public DeleteStripeExternalAccountCommandValidator(ICurrentUserService currentUserService, IApplicationDbContext dbContext)
    {
        _currentUserService = currentUserService;
        _dbContext = dbContext;

        RuleFor(e => e.Username)
            .MustAsync(AuthorizedToDelete).WithMessage("Forbidden.");
    }

    public async Task<bool> AuthorizedToDelete(string username, CancellationToken ct)
    {
        if (_currentUserService.HasRole(PulrRoles.Administrator))
        {
            return true;
        }

        var currentUser = await _currentUserService.GetUserAsync(true);

        if(currentUser.UserName != username)
        {
            return false;
        }

        return await _dbContext.Users.Where(u => u.UserName == username && 
                                                !u.IsSuspended && u.Profile.IsActive && 
                                                 u.Profile.StripeConnectedAccount != null)
                                     .AnyAsync();
    }
}