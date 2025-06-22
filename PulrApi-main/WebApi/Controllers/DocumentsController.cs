using System;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using System.IO;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class DocumentsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IAmazonS3 _s3Client;

        public DocumentsController(IConfiguration configuration)
        {
            _configuration = configuration;
            _s3Client = new AmazonS3Client(
                _configuration["Aws:AwsAccessKeyId"],
                _configuration["Aws:AwsSecretAccessKey"],
                Amazon.RegionEndpoint.MESouth1
            );
        }

        [HttpGet("terms-of-service")]
        public async Task<IActionResult> GetTermsOfService()
        {
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = "prod-pulr-logo",
                    Key = "Term of Service.pdf"
                };

                using var response = await _s3Client.GetObjectAsync(request);
                using var responseStream = response.ResponseStream;
                using var memoryStream = new MemoryStream();
                await responseStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Create a new response with minimal headers
                var result = new FileContentResult(memoryStream.ToArray(), "application/pdf");
                result.FileDownloadName = "Terms of Service.pdf";
                return result;
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving document: {ex.Message}");
            }
        }

        [HttpGet("eula-agreement")]
        public async Task<IActionResult> GetEulaAgreement()
        {
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = "prod-pulr-logo",
                    Key = "EULA Agreement.pdf"
                };

                using var response = await _s3Client.GetObjectAsync(request);
                using var responseStream = response.ResponseStream;
                using var memoryStream = new MemoryStream();
                await responseStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var result = new FileContentResult(memoryStream.ToArray(), "application/pdf");
                result.FileDownloadName = "EULA Agreement.pdf";
                return result;
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving document: {ex.Message}");
            }
        }
    }
} 