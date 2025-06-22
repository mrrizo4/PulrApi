using Core.Application.Models.BagItems;

namespace Core.Application.Models.Orders
{
    public class OrderProductAffiliateDto
    {
        public string OrderUid { get; set; }
        public BagProductExtendedDto Product { get; set; }
        public string AffiliateId { get; set; }
    }
}
