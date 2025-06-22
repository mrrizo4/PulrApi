using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class PostProductTag
    {
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [Required]
        public int PostId { get; set; }
        public Post Post { get; set; }
        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public double PositionLeftPercent { get; set; }
        public double PositionTopPercent { get; set; }
    }
}
