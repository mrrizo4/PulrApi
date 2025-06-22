using Core.Application.Interfaces;
using Core.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Stores.NotificationHandlers;

public class CreateStoreConnectedAccountNotificationHandler : INotificationHandler<StoreCreatedEvent>
{
    private readonly ILogger<CreateStoreConnectedAccountNotificationHandler> _logger;
    private readonly IConfiguration _configuration;
    private readonly IStripeService _stripeService;
    private readonly IApplicationDbContext _dbContext;

    public CreateStoreConnectedAccountNotificationHandler(
        ILogger<CreateStoreConnectedAccountNotificationHandler> logger,
        IConfiguration configuration,
        IStripeService stripeService,
        IApplicationDbContext dbContext
    )
    {
        _logger = logger;
        _configuration = configuration;
        _stripeService = stripeService;
        _dbContext = dbContext;
    }

    public async Task Handle(StoreCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            bool isPaymentEnabled = _configuration.GetValue<string>("FinanceEnabled") == "true";
            if(isPaymentEnabled == false)
            {
                _logger.LogInformation("Stripe connected account creation skipped, set FinanceEnabled = true in config");
                return;
            }

            bool isUserVerified = notification.Store.User.IsVerified;
            if( isUserVerified  == false) {
                return;
            }

            var accountId = await _stripeService.CreateConnectedAccount();

            notification.Store.StripeConnectedAccount = await _dbContext.StripeConnectedAccounts.Where(sca => sca.AccountId == accountId).SingleOrDefaultAsync();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating connected account for store");
            throw;
        }
    }
}