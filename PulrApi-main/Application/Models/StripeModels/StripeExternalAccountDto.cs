namespace Core.Application.Models.StripeModels
{
    public class StripeExternalAccountDto
    {
        public string Id { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountHolderType { get; set; }
        public string Country { get; set; }
        public string Currency { get; set; }
        public string AccountNumber { get; set; }
        public bool DefaultForCurrency { get; set; }
    }
}
