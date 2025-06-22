using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Posts.Commands;
using Core.Application.Models;

namespace Core.Application.Mediatr.Posts.Commands
{
    public class DeletePostCommand : IRequest <Unit>
    {
        [Required]
        public string Uid { get; set; }
    }

    public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand,Unit>
    {
        private readonly ILogger<DeletePostCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IFileUploadService _fileUploadService;

        public DeletePostCommandHandler(ILogger<DeletePostCommandHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext,
            IConfiguration configuration,
            IFileUploadService fileUploadService)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
            _configuration = configuration;
            _fileUploadService = fileUploadService;
        }

        public async Task<Unit> Handle(DeletePostCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync(true);
                var postToDelete = await _dbContext.Posts.Include(p => p.PostProductTags)
                    .Include(p => p.Comments)
                    .Include(p => p.PostLikes)
                    .Include(p => p.MediaFile)
                    .Include(p => p.PostHashtags)
                    .Include(p => p.PostProfileMentions)
                    .Include(p => p.PostStoreMentions)
                    .SingleOrDefaultAsync(p => p.Uid == request.Uid && p.User == cUser);
                if (postToDelete == null)
                {
                    throw new BadRequestException("");
                }

                string mediaFileUrl = postToDelete.MediaFile != null ? postToDelete.MediaFile.Url : null;
                string mediaFileUid = postToDelete.MediaFile != null ? postToDelete.MediaFile.Uid : null;

                _dbContext.Posts.Remove(postToDelete);
                await _dbContext.SaveChangesAsync(CancellationToken.None);

                if (mediaFileUrl != null)
                {
                    var fileConfig = new FileUploadConfigDto()
                    {
                        OldFileName = mediaFileUrl.Substring(mediaFileUrl.LastIndexOf("/") + 1),
                        BucketName = _configuration[AwsLocationNames.S3UploadBucket],
                        FolderPath = _configuration[AwsLocationNames.PublicUploadFolder],
                    };

                    await _fileUploadService.Delete(fileConfig);
                    var mediaFileToDelete = await _dbContext.MediaFiles.SingleOrDefaultAsync(e => e.Uid == mediaFileUid);
                    _dbContext.MediaFiles.Remove(mediaFileToDelete);
                    await _dbContext.SaveChangesAsync(CancellationToken.None);
                }
                return Unit.Value;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }

}
