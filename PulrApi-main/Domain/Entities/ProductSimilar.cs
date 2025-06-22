
using System;
using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class ProductSimilar
    {
        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        public int SimilarId { get; set; }
        public Product Similar { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
