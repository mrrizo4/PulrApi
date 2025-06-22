using System.Collections.Generic;
using Core.Application.Models.Currencies;
using Core.Application.Models.ShippingDetails;

namespace Core.Application.Models.Orders
{
    public class OrderDetailsResponse
    {
        public string Uid { get; set; }

        public string PaymentMethodUid { get; set; }
        public string ProfileUid { get; set; }
        public ShippingDetailsResponse ShippingDetails { get; set; }
        public CurrencyDetailsResponse Currency { get; set; }
        public virtual ICollection<OrderProductAffiliateDto> OrderProductAffiliates { get; set; }
    }
}
