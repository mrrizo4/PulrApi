using System.Collections.Generic;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class ProductVariation : EntityBase
    {
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int StockQuantity { get; set; }

        public virtual ICollection<ProductAttributeValue> ProductAttributeValues { get; set; }
    }
}
