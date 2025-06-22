using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Models.StripeModels;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Finances.Commands.Verify;

public class VerifyStripeIndividualCommand : IRequest <Unit>
{
    [Required]
    public string Username { get; set; }
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
    public List<StripeExternalAccountModel> ExternalAccounts { get; set; }
}

public class StripeVerifyIndividualCommandHandler : IRequestHandler<VerifyStripeIndividualCommand,Unit>
{
    private readonly ILogger<StripeVerifyIndividualCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IStripeService _stripeService;
    private readonly IMapper _mapper;

    public StripeVerifyIndividualCommandHandler(ILogger<StripeVerifyIndividualCommandHandler> logger,
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

    public async Task<Unit> Handle(VerifyStripeIndividualCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();
            var currentProfile = await _dbContext.Profiles.Include(e => e.StripeConnectedAccount)
                                                          .Where(e => e.User.UserName == request.Username)
                                                          .SingleOrDefaultAsync();

            var verificationModel = _mapper.Map<StripeIndividualVerificationDetailsDto>(request);
            verificationModel.AccountId = currentProfile.StripeConnectedAccount.AccountId;
            await _stripeService.VerifyConnectedAccountForIndividual(verificationModel, request.Username);

            var status = await _stripeService.GetUserVerificationStatus(currentProfile.StripeConnectedAccount.AccountId);

            currentUser.IsVerified = status.ChargesEnabled && status.PayoutsEnabled;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}
