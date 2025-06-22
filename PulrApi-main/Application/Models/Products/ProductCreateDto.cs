using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Application.Models.Products;

namespace Core.Application.Models.Products
{
    public class ProductCreateDto
    {
        [Required]
        public string StoreUid { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public double Price { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; } 
        public string CategoryUid { get; set; }
        public ICollection<ProductMoreInfoRequest> MoreInfos { get; set; } = new List<ProductMoreInfoRequest>();
        public ICollection<ProductAttributeCreateRequest> ProductAttributes { get; set; }
        public ICollection<string> ProductPairArticleCodes { get; set; } = new List<string>();
        public ICollection<string> ProductSimilarArticleCodes { get; set; } = new List<string>();
    }
}
