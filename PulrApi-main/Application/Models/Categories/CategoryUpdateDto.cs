
using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Categories
{
    public class CategoryUpdateDto
    {
        [Required]
        public string Uid { get; set; }
        [Required]
        public string Title { get; set; }
        public string ParentCategoryUid { get; set; }
        [Required]
        public string StoreUid { get; set; }
    }
}
