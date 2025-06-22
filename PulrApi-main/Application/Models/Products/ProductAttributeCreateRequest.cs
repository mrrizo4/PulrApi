using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Products
{
    public class ProductAttributeCreateRequest
    {
        [Required]
        public string Key { get; set; }
        [Required]
        public string Values { get; set; }
    }
}
