
namespace Core.Application.Models.Products
{
    public class ProductPublicResponse
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string StoreName { get; set; }
        public double Price { get; set; }
        public string CurrencyUid { get; set; }
        public string CurrencyCode { get; set; }
        public string FeaturedImageUrl { get; set; }
        public string AffiliateId { get; set; }
    }
}
