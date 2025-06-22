using AutoMapper;
using Core.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Finances.Commands.Delete;

public class DeleteStripeExternalAccountCommand : IRequest <Unit>
{
    [Required]
    public string Username { get; set; }
    [Required]
    public string ExternalAccountId { get; set; }
}

public class DeleteFinanceCommandHandler : IRequestHandler<DeleteStripeExternalAccountCommand,Unit>
{
    private readonly ILogger<DeleteFinanceCommandHandler> _logger;
    private readonly IStripeService _stripeService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public DeleteFinanceCommandHandler(ILogger<DeleteFinanceCommandHandler> logger, IStripeService stripeService,IApplicationDbContext dbContext, IMapper mapper)
    {
        _logger = logger;
        _stripeService = stripeService;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Unit> Handle(DeleteStripeExternalAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var accountId = await _dbContext.Profiles.Where(e => e.User.UserName == request.Username)
                                                     .Select(e => e.StripeConnectedAccount.AccountId)
                                                     .SingleOrDefaultAsync(cancellationToken);

            await _stripeService.DeleteExternalAccount(accountId, request.ExternalAccountId);

            return Unit.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}