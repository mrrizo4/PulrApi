using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Application.Security.Validation.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Core.Domain.Enums;

namespace Core.Application.Models.MediaFiles
{
    public class UploadMediaFileDto
    {
        [Required(ErrorMessage = "Files are required")]
        [MaxFileSize(30 * 1024 * 1024, "Video")] // 30MB for videos
        [FromForm(Name = "Files")]
        public List<IFormFile> Files { get; set; }
    }
}
