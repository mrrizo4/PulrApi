using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class ProductAttributeValue : EntityBase
    {
        [Required]
        public int ProductAttributeId { get; set; }
        public ProductAttribute ProductAttribute { get; set; }

        [Required]
        public string Value { get; set; }
    }
}
