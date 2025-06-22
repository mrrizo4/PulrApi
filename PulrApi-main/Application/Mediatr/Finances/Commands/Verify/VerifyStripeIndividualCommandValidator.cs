using FluentValidation;
using Core.Application.Interfaces;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.Finances.Commands.Verify;

public class VerifyStripeIndividualCommandValidator : AbstractValidator<VerifyStripeIndividualCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public VerifyStripeIndividualCommandValidator(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;

        RuleFor(e => e.Username)
            .MustAsync(ValidUsername).WithMessage("Uid can't be empty.");
    }

    public async Task<bool> ValidUsername(string username, CancellationToken ct)
    {
        return await _dbContext.Users.Where(u => u.UserName == username && !u.IsSuspended && u.Profile.IsActive && u.Profile.StripeConnectedAccount != null).AnyAsync();
    }

}