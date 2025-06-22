using System.Collections.Generic;

namespace Core.Application.Models.StripeModels
{
    public class StripeIndividualVerificationDetailsDto
    {
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
        public List<StripeExternalAccountDto> ExternalAccounts { get; set; }
    }
}
