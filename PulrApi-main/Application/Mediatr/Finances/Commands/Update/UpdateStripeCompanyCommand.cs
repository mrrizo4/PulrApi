using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Models.StripeModels;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Finances.Commands.Update;

public class UpdateStripeCompanyCommand : IRequest<Unit>
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
    public string AccountNumber { get; set; }
    public string Mcc { get; set; }
    public bool ShouldUpdateAccountUrl { get; set; }
}

public class UpdateStripeCompanyCommandHandler : IRequestHandler<UpdateStripeCompanyCommand,Unit>
{
    private readonly ILogger<UpdateStripeCompanyCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IStripeService _stripeService;
    private readonly IMapper _mapper;

    public UpdateStripeCompanyCommandHandler(ILogger<UpdateStripeCompanyCommandHandler> logger, 
        ICurrentUserService currentUserService, 
        IApplicationDbContext dbContext, 
        IStripeService stripeService, 
        IMapper mapper)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
        _stripeService = stripeService;
        _mapper = mapper;
    }

    public async Task<Unit> Handle(UpdateStripeCompanyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();
            var requestMapped = _mapper.Map<StripeCompanyUpdateDto>(request);
            await _stripeService.UpdateCompanyAccount(requestMapped, currentUser.UserName);

            return Unit.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}
