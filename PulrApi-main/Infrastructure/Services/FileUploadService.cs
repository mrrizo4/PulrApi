using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.Services;

namespace Core.Infrastructure.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(IConfiguration configuration,
            ILogger<FileUploadService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> UploadImage(FileUploadConfigDto config)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using var memoryStreamForResizedImage = new MemoryStream();
                config.File.CopyTo(memoryStream);
                ImageResizer.Resize(memoryStream, config.ImageWidth, config.ImageHeight, memoryStreamForResizedImage);
                string uploadedImageKey = await Upload(
                    memoryStreamForResizedImage,
                    String.Format("{0}{1}",
                        Guid.NewGuid().ToString(),
                        //Path.GetFileNameWithoutExtension(config.File),
                        Path.GetExtension(config.FileName)),
                     config
                );

                return uploadedImageKey;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<string> UploadVideo(FileUploadConfigDto config)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using var memoryStreamForResizedImage = new MemoryStream();
                config.File.CopyTo(memoryStream);
                string uploadedVideoKey = await Upload(
                    memoryStream,
                    String.Format("{0}{1}",
                        Guid.NewGuid().ToString(),
                        Path.GetExtension(config.FileName)),
                     config
                );

                return uploadedVideoKey;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        private async Task<string> Upload(Stream stream, string fileName, FileUploadConfigDto config)
        {
            using (var client = new AmazonS3Client(_configuration["Aws:AwsAccessKeyId"],
                       _configuration["Aws:AwsSecretAccessKey"], RegionEndpoint.MESouth1))
            {
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = stream,
                    Key = fileName,
                    BucketName = config.BucketName + "/" + config.FolderPath,
                    CannedACL = S3CannedACL.PublicRead
                };

                var fileTransferUtility = new TransferUtility(client);
                await fileTransferUtility.UploadAsync(uploadRequest);

                //string fullPath = _configuration["Aws:S3BasePath"] + "/" + config.FolderPath + "/" + uploadRequest.Key;
                var uploadedFileKey = uploadRequest.Key;
                return uploadedFileKey;
            }
        }

        public async Task<DeleteObjectResponse> Delete(FileUploadConfigDto config)
        {
            using (var client = new AmazonS3Client(_configuration["Aws:AwsAccessKeyId"],
                       _configuration["Aws:AwsSecretAccessKey"], RegionEndpoint.MESouth1))
            {

                var deleteRequest = new DeleteObjectRequest
                {
                    Key = config.OldFileName,
                    BucketName = config.BucketName + "/" + config.FolderPath
                };

                var response = await client.DeleteObjectAsync(deleteRequest);

                return response;
            }
        }
        
        public async Task ListFilesInBucket(string bucketName, string prefixOrPath)
        {
            try
            {
                using (var client = new AmazonS3Client(_configuration["Aws:AwsAccessKeyId"],
                       _configuration["Aws:AwsSecretAccessKey"], RegionEndpoint.MESouth1))
                
                {
                    var listObjectsV2Paginator = client.Paginators.ListObjectsV2(new ListObjectsV2Request
                    {
                        BucketName = bucketName,
                        Prefix = prefixOrPath,
                        MaxKeys = 10 // how many items per page
                    });

                    var currentPageItemNames = new List<string>();
                    // we loop through all pages 
                    await foreach (var response in listObjectsV2Paginator.Responses)
                    {
                        var httpStatusCode = response.HttpStatusCode;
                        var numberOfKeys = response.KeyCount;
                        currentPageItemNames = response.S3Objects.Select(o => o.Key).ToList();
                    }


                }
            }
            catch (Exception e)
            {
                throw new Exception("Error listing files in bucket", e);
            }
        }
    }
}
