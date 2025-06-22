using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class PostStoreMention
    {
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [Required]
        public int PostId { get; set; }
        public Post Post { get; set; }
        [Required]
        public int StoreId { get; set; }
        public Store Store { get; set; }
    }
}
