
using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class ProductLike
    {
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }
        [Required]
        public int LikedById { get; set; }
        public Profile LikedBy { get; set; }
    }
}
