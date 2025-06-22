using FluentValidation;
using Core.Application.Interfaces;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.Finances.Commands.Verify;
public class VerifyStripeCompanyCommandValidator : AbstractValidator<VerifyStripeCompanyCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public VerifyStripeCompanyCommandValidator(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;

        RuleFor(e => e.UniqueName)
            .MustAsync(ValidUniqueName).WithMessage("Uid can't be empty.");
    }

    public async Task<bool> ValidUniqueName(string uniqueName, CancellationToken ct)
    {
        return await _dbContext.Stores.Where(e => e.UniqueName == uniqueName && e.IsActive && e.StripeConnectedAccount != null).AnyAsync();
    }

}