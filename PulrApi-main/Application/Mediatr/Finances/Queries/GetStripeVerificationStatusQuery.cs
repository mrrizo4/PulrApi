using Core.Application.Interfaces;
using Core.Application.Models.StripeModels;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Finances.Queries;

public class GetStripeVerificationStatusQuery : IRequest<StripeIndividualVerificationStatusResponse>
{
    [Required]
    public string AccountId { get; set; }
}

public class GetFinanceQueryHandler : IRequestHandler<GetStripeVerificationStatusQuery, StripeIndividualVerificationStatusResponse>
{
    private readonly ILogger<GetFinanceQueryHandler> _logger;
    private readonly IStripeService _stripeService;

    public GetFinanceQueryHandler(ILogger<GetFinanceQueryHandler> logger, IStripeService stripeService)
    {
        _logger = logger;
        _stripeService = stripeService;
    }

    public async Task<StripeIndividualVerificationStatusResponse> Handle(GetStripeVerificationStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var status = await _stripeService.GetUserVerificationStatus(request.AccountId);
            return new StripeIndividualVerificationStatusResponse()
            {
                ChargesEnabled = status.ChargesEnabled,
                PayoutsEnabled = status.PayoutsEnabled,
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}
