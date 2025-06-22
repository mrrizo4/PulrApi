namespace Core.Application.Models.StripeModels
{
    public class StripeIndividualVerificationStatusResponse
    {
        public bool ChargesEnabled { get; set; }
        public bool PayoutsEnabled { get; set; }
    }
}
