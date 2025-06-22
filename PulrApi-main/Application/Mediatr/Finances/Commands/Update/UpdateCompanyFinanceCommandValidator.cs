using FluentValidation;
using Core.Application.Interfaces;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.Finances.Commands.Update;

public class UpdateCompanyFinanceCommandValidator : AbstractValidator<UpdateStripeCompanyCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCompanyFinanceCommandValidator(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;

        RuleFor(e => e.AccountId)
            .MustAsync(ValidAccount).WithMessage("No such user.");
    }

    public async Task<bool> ValidAccount(string accountId, CancellationToken ct)
    {
        var currentUser = await _currentUserService.GetUserAsync(true);

        return await _dbContext.Users.Where(u => u.UserName == currentUser.UserName &&
                                                !u.IsSuspended && u.Profile.IsActive &&
                                                u.Profile.StripeConnectedAccount.AccountId == accountId)
                                     .AnyAsync(ct);
    }

}
