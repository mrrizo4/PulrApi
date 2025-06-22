
namespace Core.Domain.Entities
{
    public class StripeConnectedAccount : EntityBase
    {
        public string AccountId { get; set; }
        public bool AccountTermsAccepted { get; set; }
        public string StripeAccountResponseJson { get; set; }
    }
}
