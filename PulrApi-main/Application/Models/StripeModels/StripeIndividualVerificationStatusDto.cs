namespace Core.Application.Models.StripeModels
{
    public class StripeIndividualVerificationStatusDto
    {
        public bool ChargesEnabled { get; set; }
        public bool PayoutsEnabled { get; set; }
    }
}
