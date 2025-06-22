using Core.Application.Constants;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Users.Commands.Login;

public class DashboardLoginCommandValidator : AbstractValidator<DashboardLoginCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<DashboardLoginCommandValidator> _logger;
    private readonly IApplicationDbContext _dbContext;

    public DashboardLoginCommandValidator(UserManager<User> userManager, ILogger<DashboardLoginCommandValidator> logger,
        IApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _logger = logger;
        _dbContext = dbContext;
        RuleFor(u => u.Username)
            .MustAsync(MustBeAdministrator!).WithMessage("Insufficient privileges");
    }

    private async Task<bool> MustBeAdministrator(DashboardLoginCommand command, string username, CancellationToken ct)
    {
        try
        {
            User? user;
            if (command.IsEmail)
            {
                user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == username, ct);
            }
            else
            {
                user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserName == username, ct);
            }

            if(user == null)
            {
                return false;
            }

            await user.GetRoles(_userManager);
            
            return user is not null && user.Roles.Contains(PulrRoles.Administrator);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error validating users privileges");
            throw;
        }
    }
}