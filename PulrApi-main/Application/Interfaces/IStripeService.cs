using System.Threading.Tasks;
using Core.Application.Models.StripeModels;

namespace Core.Application.Interfaces;

public interface IStripeService
{
    Task<string> CreateConnectedAccount(string countryCode = "AE", string accountType = "custom");
    Task<StripeIndividualVerificationStatusDto> GetUserVerificationStatus(string accountId);
    Task<bool> UpdateTerms(string accountId);
    Task VerifyConnectedAccountForIndividual(StripeIndividualVerificationDetailsDto model, string username);
    Task<string> VerifyConnectedAccountForCompany(StripeCompanyVerificationDetailsDto model, string uniqueName);
    Task AddCompanyAuthority(StripeCompanyOwnerDto model);
    Task UpdateIndividualAccount(StripeIndividualUpdateDto model, string username);
    Task UpdateCompanyAccount(StripeCompanyUpdateDto model, string uniqueName);
    Task<StripeInfoIndividualResponse> GetStripeInfoIndividual(string username);
    Task DeleteExternalAccount(string accountId, string externalAccountId);
}