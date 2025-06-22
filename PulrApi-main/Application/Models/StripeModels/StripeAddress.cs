namespace Core.Application.Models.StripeModels;

public class StripeAddress
{
    public string Country { get; set; }
    public string Line1 { get; set; }
    public string City { get; set; }
    public string PostalCode { get; set; }
}