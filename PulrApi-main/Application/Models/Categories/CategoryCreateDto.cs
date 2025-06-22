
using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Categories
{
    public class CategoryCreateDto
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string StoreUid { get; set; }
        public string ParentCategoryUid { get; internal set; }
    }
}
