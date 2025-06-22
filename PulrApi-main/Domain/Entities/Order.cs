using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities;

public class Order : EntityBase
{
    [Required]
    public int PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    [Required]
    public int ProfileId { get; set; }
    public Profile Profile { get; set; }
    [Required]
    public int ShippingDetailsId { get; set; }
    public ShippingDetails ShippingDetails { get; set; }
    [Required]
    public int CurrencyId { get; set; }
    public Currency Currency { get; set; }
    public OrderStatusEnum OrderStatus { get; set; }
    public string RawRequest { get; set; } // minus card data
    public virtual ICollection<OrderProductAffiliate> OrderProductAffiliates { get; set; }
}
