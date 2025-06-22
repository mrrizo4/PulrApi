namespace Core.Application.Models.StripeModels
{
    public class StripeExternalAccountModel
    {
        // if Id is null, we treat it as new card
        public string Id { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountHolderType { get; set; }
        public string Country { get; set; }
        public string Currency { get; set; }
        public string AccountNumber { get; set; }
        public bool DefaultForCurrency { get; set; }
    }
}
