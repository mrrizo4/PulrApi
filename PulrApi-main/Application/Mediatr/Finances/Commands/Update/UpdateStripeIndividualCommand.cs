using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Models.StripeModels;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Finances.Commands.Update;

public class UpdateStripeIndividualCommand : IRequest <Unit>
{
    [Required]
    public string AccountId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Country { get; set; }
    public string Line1 { get; set; }
    public string City { get; set; }
    public string PostalCode { get; set; }
    public string Phone { get; set; }
    public string DefaultCurrency { get; set; }
    public long? DayOfBirth { get; set; }
    public long? MonthOfBirth { get; set; }
    public long? YearOfBirth { get; set; }
    public string Mcc { get; set; }
    public bool ShouldUpdateAccountUrl { get; set; }
    public List<StripeExternalAccountModel> ExternalAccounts { get; set; }
}

public class UpdateStripeIndividualCommandHandler : IRequestHandler<UpdateStripeIndividualCommand,Unit>
{
    private readonly ILogger<UpdateStripeIndividualCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IStripeService _stripeService;
    private readonly IMapper _mapper;

    public UpdateStripeIndividualCommandHandler(ILogger<UpdateStripeIndividualCommandHandler> logger, ICurrentUserService currentUserService, IApplicationDbContext dbContext, IStripeService stripeService, IMapper mapper)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
        _stripeService = stripeService;
        _mapper = mapper;
    }

    public async Task<Unit> Handle(UpdateStripeIndividualCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();
            var requestMapped = _mapper.Map<StripeIndividualUpdateDto>(request);
            await _stripeService.UpdateIndividualAccount(requestMapped, currentUser.UserName);

            return Unit.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}
