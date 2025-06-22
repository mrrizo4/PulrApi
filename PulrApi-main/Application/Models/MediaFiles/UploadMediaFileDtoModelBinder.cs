using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Core.Application.Models.MediaFiles
{
    public class UploadMediaFileDtoModelBinder : IModelBinder
    {
        private readonly ILogger<UploadMediaFileDtoModelBinder> _logger;

        public UploadMediaFileDtoModelBinder(ILogger<UploadMediaFileDtoModelBinder> logger)
        {
            _logger = logger;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            _logger.LogInformation("Binding UploadMediaFileDto model");
            _logger.LogInformation($"Form keys: {string.Join(", ", bindingContext.HttpContext.Request.Form.Keys)}");
            _logger.LogInformation($"Files count: {bindingContext.HttpContext.Request.Form.Files.Count}");

            var model = new UploadMediaFileDto();
            var files = new List<IFormFile>();

            // Check if there are any files in the request
            if (bindingContext.HttpContext.Request.Form.Files.Count > 0)
            {
                // Add all files to the list
                foreach (var file in bindingContext.HttpContext.Request.Form.Files)
                {
                    files.Add(file);
                }
            }

            model.Files = files;
            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
} 