using System.Collections.Generic;
using Core.Application.Mediatr.Categories.Queries;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Profiles;

namespace Core.Application.Models.Products;

public class ProductDetailsResponse
{
    public string Uid { get; set; }
    public string Name { get; set; }
    public string ArticleCode { get; set; }
    public string Description { get; set; }
    public ICollection<ProductMoreInfoResponse> MoreInfos { get; set; } = new List<ProductMoreInfoResponse>();
    public double Price { get; set; }
    public int Quantity { get; set; }
    public IEnumerable<MediaFileDetailsResponse> ProductMediaFiles { get; set; } = new List<MediaFileDetailsResponse>();
    public string CurrencyCode { get; set; }
    public List<SingleCategoryResponse> Categories { get; set; } = new List<SingleCategoryResponse>();
    public IEnumerable<ProductAttributeResponse> ProductAttributes { get; set; } = new List<ProductAttributeResponse>();
    public IEnumerable<string> ProductPairArticleCodes { get; set; } = new List<string>();
    public IEnumerable<string> ProductSimilarArticleCodes { get; set; } = new List<string>();
    public string CategoryTitle { get; set; }
    public string StoreUid { get; set; }
    public string StoreUniqueName { get; set; }
    public ICollection<TaggedByDto> TaggedBy { get; set; } = new List<TaggedByDto>();
    public int LikesCount { get; set; }
    public bool LikedByMe { get; set; }
    public string AffiliateId { get; set; }
    public double PositionLeftPercent { get; set; }
    public double PositionTopPercent { get; set; }
}