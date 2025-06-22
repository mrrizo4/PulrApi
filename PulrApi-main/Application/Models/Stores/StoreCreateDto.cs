using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Stores
{
    public class StoreCreateDto
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string LegalName { get; set; }
        [Required]
        public string UniqueName { get; set; }
        [Required]
        public string StoreEmail { get; set; }
        [Required]
        public string CurrencyUid { get; set; }
    }
}
