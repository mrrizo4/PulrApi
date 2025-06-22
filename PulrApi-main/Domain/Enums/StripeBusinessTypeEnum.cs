using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Enums
{
    public enum StripeBusinessTypeEnum
    {
        [Display(Name = "individual")]
        Individual,
        [Display(Name = "company")]
        Company,
        [Display(Name = "non_profit")]
        NonProfit,
        [Display(Name = "government_entity")]
        GovernmentEntity
    }
}
