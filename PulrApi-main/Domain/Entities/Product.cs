using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class Product : EntityBase
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string ArticleCode { get; set; }

        public string Description { get; set; }
        public double Price { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;

        public int? BrandId { get; set; }
        public Brand Brand { get; set; }

        public int StoreId { get; set; }

        [Required]
        public Store Store { get; set; }

        public int? ProductCategoryId { get; set; }
        public ICollection<ProductCategory> ProductCategory { get; set; } = new List<ProductCategory>();
        public OrderProductAffiliate OrderProductAffiliate { get; set; }
        public virtual ICollection<ProductMoreInfo> ProductMoreInfos { get; set; }
        public virtual ICollection<ProductMediaFile> ProductMediaFiles { get; set; }
        public virtual ICollection<ProductPair> ProductPairs { get; set; }
        public virtual ICollection<ProductSimilar> ProductSimilars { get; set; }
        public virtual ICollection<Comment> Comments { get; set; }
        public virtual ICollection<ProductLike> ProductLikes { get; set; }
        public virtual ICollection<ProductAttribute> ProductAttributes { get; set; }
        public virtual ICollection<ProductVariation> ProductVariations { get; set; }
        public virtual ICollection<ProductOnboardingPreference> ProductOnboardingPreferences { get; set; }
        public virtual ICollection<StoryProductTag> SpotProductTags { get; set; } = new List<StoryProductTag>();

        public virtual ICollection<ProductSubCategoryLevel2> ProductSubCategoryLevel2 { get; set; } =
            new List<ProductSubCategoryLevel2>();
    }
}