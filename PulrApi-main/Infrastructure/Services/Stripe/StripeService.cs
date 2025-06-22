using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Models.StripeModels;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Stripe;

namespace Core.Infrastructure.Services.Stripe;

public class StripeService : IStripeService
{
    private readonly IStripeClient _stripeClient;
    private readonly ILogger<StripeService> _logger;
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;

    public StripeService(IStripeClient stripeClient, ILogger<StripeService> logger, IMediator mediator,
        IConfiguration configuration, IApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor, IMapper mapper)
    {
        _stripeClient = stripeClient;
        _logger = logger;
        _mediator = mediator;
        _configuration = configuration;
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _mapper = mapper;
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    //TODO Do user/store verification before creating connected account
    public async Task<string> CreateConnectedAccount(string countryCode, string accountType = "custom")
    {
        var options = new AccountCreateOptions
        {
            Type = accountType,
            Country = "GB",
            Capabilities = new AccountCapabilitiesOptions
            {
                CardPayments = new AccountCapabilitiesCardPaymentsOptions
                {
                    Requested = true
                },
                Transfers = new AccountCapabilitiesTransfersOptions //enables transfers for account
                {
                    Requested = true
                }
            }
        };

        var service = new AccountService();
        var account = await service.CreateAsync(options);

        var stripeConnectedAccount = new StripeConnectedAccount()
        {
            Uid = Guid.NewGuid().ToString(),
            AccountId = account.Id,
            AccountTermsAccepted = false,
            StripeAccountResponseJson = JsonConvert.SerializeObject(account),
        };

        _dbContext.StripeConnectedAccounts.Add(stripeConnectedAccount);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        return account.Id;
    }

    public async Task<StripeIndividualVerificationStatusDto> GetUserVerificationStatus(string accountId)
    {
        try
        {
            var service = new AccountService();
            var account = await service.GetAsync(accountId);
            return new StripeIndividualVerificationStatusDto() { 
                ChargesEnabled = account.ChargesEnabled,
                PayoutsEnabled = account.PayoutsEnabled
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw;
        }
    }

    public async Task<bool> UpdateTerms(string accountId)
    {
        try
        {
            var options = new AccountUpdateOptions
            {
                TosAcceptance = new AccountTosAcceptanceOptions
                {
                    Date = DateTimeOffset.FromUnixTimeSeconds(1609798905).UtcDateTime,
                    Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString(),
                },
            };
            var service = new AccountService();
            var resultAccount = await service.UpdateAsync(accountId, options);

            var stripeConnectedAccount = await _dbContext.StripeConnectedAccounts
                .Where(e => e.AccountId == resultAccount.Id).SingleOrDefaultAsync();
            if (stripeConnectedAccount == null)
            {
                throw new NotFoundException("stripeConnectedAccount not found");
            }

            stripeConnectedAccount.StripeAccountResponseJson = JsonConvert.SerializeObject(resultAccount);
            await _dbContext.SaveChangesAsync(CancellationToken.None);

            return resultAccount.TosAcceptance == null;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw;
        }
    }

    public async Task VerifyConnectedAccountForIndividual(StripeIndividualVerificationDetailsDto model,
        string username)
    {
        try
        {
            var options = new AccountUpdateOptions()
            {
                BusinessProfile = new AccountBusinessProfileOptions()
                {
                    Url = HttpUtility.UrlEncode("https://dev.pulr.co" + "/profile/" + username), //HttpUtility.UrlEncode(_configuration["ConsumerUrls:WebApp"] + "/profile/" + username),
                    Mcc = StripeConstants.Mccs.mens_womens_clothing_stores.ToString()
                },
                BusinessType = StripeBusinessTypeEnum.Individual.GetEnumDisplayName(),
                Individual = new AccountIndividualOptions()
                {
                    // required fields for verification
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Address = new AddressOptions()
                    {
                        Country = model.Country,
                        Line1 = model.Line1,
                        City = model.City,
                        PostalCode = model.PostalCode,
                    },
                    Phone = model.Phone,
                    Dob = new DobOptions()
                    {
                        Day = model.DayOfBirth,
                        Month = model.MonthOfBirth,
                        Year = model.YearOfBirth
                    }
                },
                DefaultCurrency = model.DefaultCurrency,
            };

            var service = new AccountService();
            var connectedAccount = await service.UpdateAsync(model.AccountId, options);

            // manage external accounts
            await ManageExternalAccounts(model.ExternalAccounts, model.AccountId);

            var stripeConnectedAccount = await _dbContext.StripeConnectedAccounts
                .Where(e => e.AccountId == connectedAccount.Id).SingleOrDefaultAsync();
            if (stripeConnectedAccount == null)
            {
                throw new NotFoundException("stripeConnectedAccount not found");
            }

            stripeConnectedAccount.StripeAccountResponseJson = JsonConvert.SerializeObject(connectedAccount);
            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw;
        }
    }

    private async Task ManageExternalAccounts(List<StripeExternalAccountDto> externalAccounts, string connectedAccountId)
    {
        try
        {
            if (!externalAccounts.Any()) { return; }

            for (int i = 0; i < externalAccounts.Count; i++)
            {

                if (String.IsNullOrWhiteSpace(externalAccounts[i].Id))
                {
                    await this.CreateExternalAccount(externalAccounts[i], connectedAccountId);
                }
                else
                {
                    await this.UpdateExternalAccount(externalAccounts[i], connectedAccountId);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw;
        }

    }

    private async Task CreateExternalAccount(StripeExternalAccountDto externalAccount, string connectedAccountId)
    {
        try
        {
            var externalOptions = new ExternalAccountCreateOptions
            {
                ExternalAccount = new AccountBankAccountOptions()
                {
                    AccountHolderName = externalAccount.AccountHolderName,
                    AccountHolderType = externalAccount.AccountHolderType,
                    Country = externalAccount.Country,
                    Currency = externalAccount.Currency,
                    AccountNumber = externalAccount.AccountNumber //"GB82WEST12345698765432" // Test Account (GB) for success
                },
                DefaultForCurrency = externalAccount.DefaultForCurrency
            };
            var externalService = new ExternalAccountService();
            await externalService.CreateAsync(connectedAccountId, externalOptions);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw;
        }
    }

    // Stripe info: card number not updatable by design
    private async Task UpdateExternalAccount(StripeExternalAccountDto externalAccount, string connectedAccountId)
    {
        try
        {
            var externalOptions = new ExternalAccountUpdateOptions
            {
                AccountHolderName = externalAccount.AccountHolderName,
                AccountHolderType = externalAccount.AccountHolderType,
                DefaultForCurrency = externalAccount.DefaultForCurrency
            };
            var externalService = new ExternalAccountService();
            await externalService.UpdateAsync(connectedAccountId, externalAccount.Id, externalOptions);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw;
        }
    }

    public async Task UpdateIndividualAccount(StripeIndividualUpdateDto model, string username)
    {
        try
        {
            var options = new AccountUpdateOptions();
            options.BusinessProfile = new AccountBusinessProfileOptions();
            if (model.ShouldUpdateAccountUrl)
            {
                options.BusinessProfile.Url = HttpUtility.UrlEncode("https://dev.pulr.co" + "/profile/" + username);
            }
            if (!String.IsNullOrWhiteSpace(model.Mcc))
            {
                options.BusinessProfile.Mcc = model.Mcc;
            }

            options.Individual = new AccountIndividualOptions();
            options.Individual.FirstName = !String.IsNullOrWhiteSpace(model.FirstName) ? model.FirstName : null;
            options.Individual.LastName = !String.IsNullOrWhiteSpace(model.LastName) ? model.LastName : null;
            options.Individual.Email = !String.IsNullOrWhiteSpace(model.Email) ? model.Email : null;
            options.Individual.Address = new AddressOptions();
            options.Individual.Address.Country = !String.IsNullOrWhiteSpace(model.Country) ? model.Country : null;
            options.Individual.Address.Line1 = !String.IsNullOrWhiteSpace(model.Line1) ? model.Line1 : null;
            options.Individual.Address.City = !String.IsNullOrWhiteSpace(model.City) ? model.City : null;
            options.Individual.Address.PostalCode = !String.IsNullOrWhiteSpace(model.PostalCode) ? model.PostalCode : null;
            options.Individual.Phone = !String.IsNullOrWhiteSpace(model.Phone) ? model.Phone : null;
            options.Individual.Dob = new DobOptions();
            options.Individual.Dob.Day = model.DayOfBirth;
            options.Individual.Dob.Month = model.MonthOfBirth;
            options.Individual.Dob.Year = model.YearOfBirth;
            options.DefaultCurrency = !String.IsNullOrWhiteSpace(model.DefaultCurrency) ? model.DefaultCurrency : null;

            var service = new AccountService();
            var connectedAccount = await service.UpdateAsync(model.AccountId, options);

            // card details are not updatable by design
            await this.ManageExternalAccounts(model.ExternalAccounts, connectedAccount.Id);

            var stripeConnectedAccount = await _dbContext.StripeConnectedAccounts
                .Where(e => e.AccountId == connectedAccount.Id).SingleOrDefaultAsync();
            if (stripeConnectedAccount == null)
            {
                throw new NotFoundException("stripeConnectedAccount not found");
            }

            stripeConnectedAccount.StripeAccountResponseJson = JsonConvert.SerializeObject(connectedAccount);
            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }
    }

    public async Task UpdateCompanyAccount(StripeCompanyUpdateDto model, string uniqueName)
    {
        try
        {
            var options = new AccountUpdateOptions();
            options.BusinessProfile = new AccountBusinessProfileOptions();
            if (model.ShouldUpdateAccountUrl)
            {
                options.BusinessProfile.Url = HttpUtility.UrlEncode("https://dev.pulr.co" + "/store/" + uniqueName);
            }
            if (!String.IsNullOrWhiteSpace(model.Mcc))
            {
                options.BusinessProfile.Mcc = model.Mcc;
            }

            options.Individual = new AccountIndividualOptions();
            options.Individual.FirstName = !String.IsNullOrWhiteSpace(model.FirstName) ? model.FirstName : null;
            options.Individual.LastName = !String.IsNullOrWhiteSpace(model.LastName) ? model.LastName : null;
            options.Individual.Email = !String.IsNullOrWhiteSpace(model.Email) ? model.Email : null;
            options.Individual.Address = new AddressOptions();
            options.Individual.Address.Country = !String.IsNullOrWhiteSpace(model.Country) ? model.Country : null;
            options.Individual.Address.Line1 = !String.IsNullOrWhiteSpace(model.Line1) ? model.Line1 : null;
            options.Individual.Address.City = !String.IsNullOrWhiteSpace(model.City) ? model.City : null;
            options.Individual.Address.PostalCode = !String.IsNullOrWhiteSpace(model.PostalCode) ? model.PostalCode : null;
            options.Individual.Phone = !String.IsNullOrWhiteSpace(model.Phone) ? model.Phone : null;
            options.Individual.Dob = new DobOptions();
            options.Individual.Dob.Day = model.DayOfBirth;
            options.Individual.Dob.Month = model.MonthOfBirth;
            options.Individual.Dob.Year = model.YearOfBirth;
            options.DefaultCurrency = !String.IsNullOrWhiteSpace(model.DefaultCurrency) ? model.DefaultCurrency : null;

            var service = new AccountService();
            var connectedAccount = await service.UpdateAsync(model.AccountId, options);

            var stripeConnectedAccount = await _dbContext.StripeConnectedAccounts
                .Where(e => e.AccountId == connectedAccount.Id).SingleOrDefaultAsync();
            if (stripeConnectedAccount == null)
            {
                throw new NotFoundException("stripeConnectedAccount not found");
            }

            stripeConnectedAccount.StripeAccountResponseJson = JsonConvert.SerializeObject(connectedAccount);
            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }
    }

    public async Task<string> VerifyConnectedAccountForCompany(StripeCompanyVerificationDetailsDto model,
        string uniqueName)
    {
        try
        {
            var options = new AccountUpdateOptions
            {
                BusinessType = StripeBusinessTypeEnum.Company.GetEnumDisplayName().ToLower(),
                Company = new AccountCompanyOptions
                {
                    Name = model.LegalBusinessName,
                    RegistrationNumber = model.CompaniesHouseRegistrationNumber,
                    Address = new AddressOptions
                    {
                        Country = model.RegisteredBusinessAddress.Country,
                        Line1 = model.RegisteredBusinessAddress.Line1,
                        City = model.RegisteredBusinessAddress.City,
                        PostalCode = model.RegisteredBusinessAddress.PostalCode
                    },
                    Phone = model.Phone,
                    OwnersProvided = true
                },
                BusinessProfile = new AccountBusinessProfileOptions()
                {
                    Url = HttpUtility.UrlEncode("https://dev.pulr.co" + "/store/" + uniqueName),
                    Mcc = StripeConstants.Mccs.mens_womens_clothing_stores.ToString()
                }
            };

            var service = new AccountService();
            var connectedAccount = await service.UpdateAsync(model.AccountId, options);

            var stripeConnectedAccount = await _dbContext.StripeConnectedAccounts
                .Where(e => e.AccountId == connectedAccount.Id)
                .SingleOrDefaultAsync();

            if (stripeConnectedAccount == null)
            {
                throw new NotFoundException("Stripe connected account not found");
            }

            stripeConnectedAccount.StripeAccountResponseJson = JsonConvert.SerializeObject(connectedAccount);
            await _dbContext.SaveChangesAsync(CancellationToken.None);
            return connectedAccount.Id;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }

    public async Task AddCompanyAuthority(StripeCompanyOwnerDto model)
    {
        try
        {
            var person = new PersonCreateOptions
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Address = new AddressOptions
                {
                    City = model.Address.City,
                    PostalCode = model.Address.PostalCode,
                    Line1 = model.Address.Line1,
                    Country = model.Address.Country
                },
                Dob = new DobOptions
                {
                    Day = model.DateOfBirth.Day,
                    Month = model.DateOfBirth.Month,
                    Year = model.DateOfBirth.Year,
                },
                Phone = model.Phone,
                Relationship = new PersonRelationshipOptions()
                {
                    PercentOwnership = model.OwnershipPercent,
                    Title = model.JobTitle
                }
            };

            var service = new PersonService();
            var personResponse = await service.CreateAsync(model.ParentId, person);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }

    public async Task<StripeInfoIndividualResponse> GetStripeInfoIndividual(string username)
    {
        try
        {
            var accountId = await _dbContext.Users.Where(u => u.UserName == username).Select(u => u.Profile.StripeConnectedAccount.AccountId).SingleOrDefaultAsync();
            if (accountId == null)
            {
                throw new NotFoundException("No such account");
            }
            var service = new AccountService();
            var account = await service.GetAsync(accountId);

            // We have to save default external account, cause connected account might have many external accounts.

            var cards = new List<StripeCardResponse>();
            foreach (var externalAccount in account.ExternalAccounts)
            {
                cards.Add(JsonConvert.DeserializeObject<StripeCardResponse>(JsonConvert.SerializeObject(externalAccount)));
            }

            return new StripeInfoIndividualResponse()
            {
                AccountId = accountId,
                FirstName = account.Individual.FirstName,
                LastName = account.Individual.LastName,
                Email = account.Individual.Email,
                Country = account.Individual.Address.Country,
                Line1 = account.Individual.Address.Line1,
                City = account.Individual.Address.City,
                PostalCode = account.Individual.Address.PostalCode,
                Phone = account.Individual.Address.PostalCode,
                DefaultCurrency = account.DefaultCurrency,
                DayOfBirth = account.Individual.Dob.Day,
                MonthOfBirth = account.Individual.Dob.Month,
                YearOfBirth = account.Individual.Dob.Year,
                BusinessProfileUrl = account.BusinessProfile.Url,
                BusinessProfileMcc = account.BusinessProfile.Mcc,
                Cards = cards
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw;
        }
    }

    public async Task DeleteExternalAccount(string accountId, string externalAccountId)
    {
        try
        {
            var service = new CardService();
            await service.DeleteAsync(accountId, externalAccountId);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw;
        }
    }
}