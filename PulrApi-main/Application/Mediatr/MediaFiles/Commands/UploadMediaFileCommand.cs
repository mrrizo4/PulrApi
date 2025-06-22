using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Mediatr.MediaFiles.Commands;
using Core.Application.Models;
using Core.Application.Models.MediaFiles;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.MediaFiles.Commands
{
    public class UploadMediaFileCommand : IRequest<List<MediaFileDetailsResponse>>
    {
        [Required, MaxFileSize(10 * 1024 * 1024), PulrFileValidation]
        public List<IFormFile> Files { get; set; }
    }

    public class AddMediaFileToPostCommandHandler : IRequestHandler<UploadMediaFileCommand, List<MediaFileDetailsResponse>>
    {
        private readonly ILogger<AddMediaFileToPostCommandHandler> _logger;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IConfiguration _configuration;
        private readonly IApplicationDbContext _dbContext;
        private readonly IFileUploadService _fileUploadService;

        public AddMediaFileToPostCommandHandler(ILogger<AddMediaFileToPostCommandHandler> logger,
            IMapper mapper, ICurrentUserService currentUserService, IConfiguration configuration, IApplicationDbContext dbContext, IFileUploadService fileUploadService)
        {
            _logger = logger;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _configuration = configuration;
            _dbContext = dbContext;
            _fileUploadService = fileUploadService;
        }
        public async Task<List<MediaFileDetailsResponse>> Handle(UploadMediaFileCommand request, CancellationToken cancellationToken)
        {
            try
            {
                //var model = _mapper.Map<UploadMediaFileDto>(request);
                var currentUser = await _currentUserService.GetUserAsync(true);
                var response = new List<MediaFileDetailsResponse>();

                //var fileTypeInfo = FileHelper.CheckFile(model.File);
                string bucketName = _configuration[AwsLocationNames.S3UploadBucket];
                string folderPath = _configuration[AwsLocationNames.PublicUploadFolder];

                foreach (var file in request.Files)
                {
                    var fileTypeInfo = FileHelper.CheckFile(file);
                    var fileConfig = new FileUploadConfigDto()
                    {
                        FileName = fileTypeInfo.Name,
                        BucketName = bucketName,
                        FolderPath = folderPath,
                        File = file,
                        ImageWidth = PulrGlobalConfig.PostImage.Width,
                        ImageHeight = PulrGlobalConfig.PostImage.Height
                    };
                    var mediaFile = new MediaFile()
                    {
                        Priority = 0,
                        MediaFileType = FileHelper.FileTypeEnumToMediaFileTypeEnum(fileTypeInfo.FileType),
                        Url = fileTypeInfo.FileType == FileTypeEnum.Image 
                        ? await _fileUploadService.UploadImage(fileConfig) 
                        : await _fileUploadService.UploadVideo(fileConfig),
                        Uid = Guid.NewGuid().ToString()
                    };

                    _dbContext.MediaFiles.Add(mediaFile);
                    response.Add(_mapper.Map<MediaFileDetailsResponse>(mediaFile));
                }             

                 

                await _dbContext.SaveChangesAsync(CancellationToken.None);

                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }


}
