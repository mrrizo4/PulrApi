using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Models.StripeModels;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Finances.Commands.Verify;

public class VerifyStripeCompanyCommand : IRequest <Unit>
{
    [Required] public string UniqueName { get; set; }
    public string LegalBusinessName { get; set; }
    public string Country { get; set; }
    public string Line1 { get; set; }
    public string City { get; set; }
    public string PostalCode { get; set; }
    public string Phone { get; set; }
    public string DefaultCurrency { get; set; }
    public string AccountNumber { get; set; }
}

public class VerifyStripeCompanyCommandHandler : IRequestHandler<VerifyStripeCompanyCommand,Unit>
{
    private readonly ILogger<VerifyStripeCompanyCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IStripeService _stripeService;
    private readonly IMapper _mapper;

    public VerifyStripeCompanyCommandHandler(ILogger<VerifyStripeCompanyCommandHandler> logger,
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

    public async Task<Unit> Handle(VerifyStripeCompanyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var store = await _dbContext.Stores
                .AsSplitQuery()
                .Include(e => e.StripeConnectedAccount)
                .Include(e => e.User).ThenInclude(u => u.Country)
                .SingleOrDefaultAsync(e => e.UniqueName == request.UniqueName, cancellationToken);

            if (store == null)
                throw new NotFoundException("Store not found");

            var verificationModel = _mapper.Map<StripeCompanyVerificationDetailsDto>(request);
            verificationModel.RegisteredBusinessAddress = _mapper.Map<StripeAddress>(request);
            verificationModel.AccountId = store.StripeConnectedAccount.AccountId;

            //get owner data
            var storeOwner = new StripeCompanyOwnerDto();
            storeOwner.ParentId =
                await _stripeService.VerifyConnectedAccountForCompany(verificationModel, request.UniqueName);
                
            storeOwner.Phone = store.User.PhoneNumber;
            storeOwner.Email = store.User.Email;
            storeOwner.FirstName = store.User.FirstName;
            storeOwner.LastName = store.User.LastName;
            storeOwner.JobTitle = StripeAuthorityEnum.CEO.GetEnumDisplayName();
            storeOwner.OwnershipPercent = 100; //TODO read this property from request  
            storeOwner.DateOfBirth = store.User.DateOfBirth.Date;
            storeOwner.Address = new StripeAddress
            {
                Country = store.User.Country.Iso2,
                City = store.User.CityName,
                Line1 = store.User.Address,
                PostalCode = store.User.ZipCode
            };

            await _stripeService.AddCompanyAuthority(storeOwner);
            var status = await _stripeService.GetUserVerificationStatus(store.StripeConnectedAccount.AccountId);

            store.IsVerified = status.ChargesEnabled && status.PayoutsEnabled;

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