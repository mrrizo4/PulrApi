using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Stores.Notifications;

public class VerifyUserStoresNotification : INotification
{
    public User? User { get; set; }
}

public class VerifyUserStoresNotificationHandler : INotificationHandler<VerifyUserStoresNotification>
{
    private readonly ILogger<VerifyUserStoresNotificationHandler> _logger;
    private readonly IStripeService _stripeService;
    private readonly IApplicationDbContext _dbContext;

    public VerifyUserStoresNotificationHandler(
        ILogger<VerifyUserStoresNotificationHandler> logger,
        IStripeService stripeService,
        IApplicationDbContext dbContext)
    {
        _logger = logger;
        _stripeService = stripeService;
        _dbContext = dbContext;
    }

    public async Task Handle(VerifyUserStoresNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var stores = await _dbContext.Stores
                .Include(s => s.StripeConnectedAccount)
                .Where(s => s.IsActive && s.User.Id == notification.User!.Id)
                .ToListAsync(cancellationToken);

            var user = await _dbContext.Users
                .AsSplitQuery()
                .Include(u => u.Country)
                .SingleOrDefaultAsync(u => u.Id == notification.User!.Id, cancellationToken);

            foreach (var store in stores)
            {
                if (store.StripeConnectedAccount == null)
                {
                    var stripeAccountId = await _stripeService.CreateConnectedAccount("UK");
                    store.StripeConnectedAccount = await _dbContext.StripeConnectedAccounts.SingleOrDefaultAsync(e => e.AccountId == stripeAccountId, cancellationToken);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error verifying user stores");
            throw;
        }
    }
}