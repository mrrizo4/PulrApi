using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Post
{
    public class PostProductTagDto
    {
        [Required]
        public string ProductUid { get; set; }
        public double PositionLeftPercent { get; set; }
        public double PositionTopPercent { get; set; }
    }
}
