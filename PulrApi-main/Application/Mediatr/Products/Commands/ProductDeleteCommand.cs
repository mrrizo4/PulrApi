using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Products.Commands;
using Core.Application.Models;

namespace Core.Application.Mediatr.Products.Commands
{
    public class ProductDeleteCommand : IRequest <Unit>
    {
        [Required] public string Uid { get; set; }
    }

    public class ProductDeleteCommandHandler : IRequestHandler<ProductDeleteCommand,Unit>
    {
        private readonly ILogger<ProductDeleteCommandHandler> _logger;
        private readonly IConfiguration _configuration;
        private readonly IFileUploadService _fileUploadService;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public ProductDeleteCommandHandler(
            ILogger<ProductDeleteCommandHandler> logger,
            IConfiguration configuration,
            IFileUploadService fileUploadService,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _configuration = configuration;
            _fileUploadService = fileUploadService;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(ProductDeleteCommand request, CancellationToken cancellationToken)
        {
            var itemToDelete = await _dbContext.Products
                .AsSplitQuery()
                .Include(p => p.Comments)
                .Include(p => p.ProductLikes)
                .Include(p => p.ProductMoreInfos)
                .Include(p => p.ProductPairs)
                .Include(p => p.ProductMediaFiles).ThenInclude(pmf => pmf.MediaFile)
                .Include(p => p.ProductAttributes)
                .SingleOrDefaultAsync(e => e.Uid == request.Uid && e.Store.User.Id == _currentUserService.GetUserId(),cancellationToken);
         
            if (itemToDelete == null)
            {
                _logger.LogWarning($"User with Id '{_currentUserService.GetUserId()}' tried to delete product with uid '{request.Uid}'.");
                throw new BadRequestException("Product doesn't exist.");
            }

            if (itemToDelete.ProductMediaFiles.Any())
            {
                string bucketName = _configuration[AwsLocationNames.S3UploadBucket];
                string folderPath = _configuration[AwsLocationNames.PublicUploadFolder];

                var fileConfig = new FileUploadConfigDto()
                {
                    BucketName = bucketName,
                    FolderPath = folderPath
                };

                foreach (var pmf in itemToDelete.ProductMediaFiles)
                {
                    fileConfig.OldFileName = pmf.MediaFile.Url.Substring(pmf.MediaFile.Url.LastIndexOf("/") + 1);
                    await _fileUploadService.Delete(fileConfig);
                }
            }

            _dbContext.Products.Remove(itemToDelete);
            await _dbContext.SaveChangesAsync(CancellationToken.None);
            return Unit.Value;
        }
    }
}
