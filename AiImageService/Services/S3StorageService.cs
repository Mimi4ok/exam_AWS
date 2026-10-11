using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AiImageService.Services
{
    public class S3StorageService : IStorageService
    {
        private readonly IConfiguration _configuration;

        public S3StorageService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var bucketName = _configuration["AWS:S3BucketName"];
            var region = _configuration["AWS:Region"];
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";

            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
            };

            using var s3Client = new AmazonS3Client(
                _configuration["AWS:AccessKey"],
                _configuration["AWS:SecretKey"],
                config
            );

            var fileTransferUtility = new TransferUtility(s3Client);
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = uniqueFileName,
                BucketName = bucketName,
                ContentType = contentType
                // Прибрано CannedACL, щоб уникнути помилок із вимкненими ACL
            };

            await fileTransferUtility.UploadAsync(uploadRequest);

            return $"https://{bucketName}.s3.{region}.amazonaws.com/{uniqueFileName}";
        }
    }
}