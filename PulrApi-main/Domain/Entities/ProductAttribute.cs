using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class ProductAttribute : EntityBase
    {
        [Required]
        public Product Product { get; set; }

        [Required]
        // COLOR
        public string Key { get; set; }
        // RED | GREEN | BLUe

        public ICollection<ProductAttributeValue> ProductAttributeValues { get; set; }
    }
}
