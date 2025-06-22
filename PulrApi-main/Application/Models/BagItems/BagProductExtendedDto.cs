using Core.Application.Models.Products;

namespace Core.Application.Models.BagItems
{
    public class BagProductExtendedDto : ProductDetailsResponse
    {
        public int BagQuantity { get; set; }
    }
}
