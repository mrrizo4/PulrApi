namespace Core.Application.Models.StripeModels;

public class StripeCompanyVerificationDetailsDto
{
    public string AccountId { get; set; }
    public string LegalBusinessName { get; set; }
    public string CompaniesHouseRegistrationNumber { get; set; }
    public StripeAddress RegisteredBusinessAddress { get; set; }
    public string Phone { get; set; }
    public string Industry { get; set; }
    public string BusinessWebsite { get; set; }
    //owner details
}