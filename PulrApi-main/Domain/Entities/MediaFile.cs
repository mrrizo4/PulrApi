using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities
{
    public class MediaFile : EntityBase
    {
        [Required]
        public string Url { get; set; }
        [Required]
        public MediaFileTypeEnum MediaFileType { get; set; }
        [Required]
        public int Priority { get; set; } = 0;
    }
}
