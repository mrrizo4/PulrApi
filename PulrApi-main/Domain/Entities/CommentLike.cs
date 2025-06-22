
using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class CommentLike
    {
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [Required]
        public int CommentId { get; set; }
        public Comment Comment { get; set; }
        [Required]
        public int LikedById { get; set; }
        public Profile LikedBy { get; set; }
    }
}
