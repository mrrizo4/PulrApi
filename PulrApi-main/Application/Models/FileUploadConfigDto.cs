﻿
using Microsoft.AspNetCore.Http;

namespace Core.Application.Models
{
    public class FileUploadConfigDto
    {
        public IFormFile File { get; set; }
        public string FileName { get; set; }
        public string OldFileName { get; set; }
        public string BucketName { get; set; }
        public string FolderPath { get; set; }

        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
    }
}
