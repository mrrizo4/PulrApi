using Core.Domain.Entities;

namespace Core.Domain.Entities;

public class OrderProductAffiliate : EntityBase
{
    public int OrderId { get; set; }
    public Order Order { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; }
    public int ProductQuantity { get; set; }

    public int? AffiliateId { get; set; }
    public Affiliate Affiliate { get; set; }
}
