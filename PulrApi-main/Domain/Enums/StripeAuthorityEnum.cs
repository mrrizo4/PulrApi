
using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Enums
{
    public enum StripeAuthorityEnum
    {
        [Display(Name="ceo")]
        CEO, 
        [Display(Name="support")]
        Support, 
        [Display(Name="engineer")]
        Engineer
    }
}
