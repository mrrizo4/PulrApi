using Core.Application.Mappings;

namespace Core.Application.Models.ShippingDetails
{
    public class ShippingDetailsResponse : IMapFrom<Domain.Entities.ShippingDetails>
    {
        public string Uid { get; set; }
        public string FullName { get; set; } 
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Floor { get; set; }
        public string Apartment { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }
        public string PhoneNumber { get; set; }
        public bool DefaultShippingAddress { get; set; }
    }
}
