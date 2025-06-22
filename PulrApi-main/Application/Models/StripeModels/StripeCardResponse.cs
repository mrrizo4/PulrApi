
using Newtonsoft.Json;

namespace Core.Application.Models.StripeModels
{
    public class StripeCardResponse
    {
        public string Id { get; set; }
        [JsonProperty("account_holder_name")]
        public string AccountHolderName { get; set; }
        [JsonProperty("account_holder_type")]
        public string AccountHolderType { get; set; }
        [JsonProperty("bank_name")]
        public string BankName { get; set; }
        public string Last4 { get; set;}
        public string Country { get; set;}
        public string Currency { get; set;}
        [JsonProperty("default_for_currency")]
        public bool DefaultForCurrency { get; set;}
        public string Status { get; set; }
    }
}
