using Core.Application.Interfaces;
using Core.Application.Models.StripeModels;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Finances.Queries
{
    public class GetStripeInfoIndividualQuery : IRequest<StripeInfoIndividualResponse>
    {
        [Required]
        public string Username { get; set; }
    }

    public class GetStripeInfoIndividualQueryHandler : IRequestHandler<GetStripeInfoIndividualQuery, StripeInfoIndividualResponse>
    {
        private readonly ILogger<GetStripeInfoIndividualQueryHandler> _logger;
        private readonly IStripeService _stripeService;

        public GetStripeInfoIndividualQueryHandler(ILogger<GetStripeInfoIndividualQueryHandler> logger, IStripeService stripeService)
        {
            _logger = logger;
            _stripeService = stripeService;
        }

        public async Task<StripeInfoIndividualResponse> Handle(GetStripeInfoIndividualQuery request, CancellationToken cancellationToken)
        {
            try
            {
                return await _stripeService.GetStripeInfoIndividual(request.Username);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }

}
