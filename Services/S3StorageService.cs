using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace PicBed.Services
{
    public class S3StorageService : IS3StorageService
    {
        private readonly IAmazonS3? _s3Client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<S3StorageService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly string _bucketName;
        private readonly bool _useLocalStorage;

        public S3StorageService(IAmazonS3? s3Client, IConfiguration configuration, ILogger<S3StorageService> logger, IWebHostEnvironment environment)
        {
            _s3Client = s3Client;
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
            _bucketName = _configuration["AWS:S3BucketName"] ?? "local-storage";
            
            // Check if we have valid AWS credentials
            var accessKey = _configuration["AWS:AccessKey"];
            var secretKey = _configuration["AWS:SecretKey"];
            _useLocalStorage = string.IsNullOrEmpty(accessKey) || accessKey == "test-access-key" || 
                              string.IsNullOrEmpty(secretKey) || secretKey == "test-secret-key";
            
            if (_useLocalStorage)
            {
                _logger.LogInformation("Using local file storage for development");
            }
            else
            {
                _logger.LogInformation("Using AWS S3 storage");
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file, string fileName)
        {
            try
            {
                if (_useLocalStorage)
                {
                    // Use local file storage for development
                    var uploadPath = Path.Combine(_environment.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploadPath);
                    
                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    
                    _logger.LogInformation("File {FileName} uploaded to local storage successfully", fileName);
                    return fileName;
                }
                else
                {
                    // Use S3 for production
                    if (_s3Client == null)
                        throw new InvalidOperationException("S3 client is not configured");
                        
                    using var stream = file.OpenReadStream();
                    var request = new PutObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = fileName,
                        InputStream = stream,
                        ContentType = file.ContentType,
                        ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                    };

                    await _s3Client.PutObjectAsync(request);
                    _logger.LogInformation("File {FileName} uploaded to S3 successfully", fileName);
                    
                    return fileName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName}", fileName);
                throw;
            }
        }

        public async Task<Stream?> GetFileStreamAsync(string fileName)
        {
            try
            {
                if (_useLocalStorage)
                {
                    // Use local file storage for development
                    var uploadPath = Path.Combine(_environment.WebRootPath, "uploads");
                    var filePath = Path.Combine(uploadPath, fileName);
                    
                    if (!File.Exists(filePath))
                    {
                        _logger.LogWarning("File {FileName} not found in local storage", fileName);
                        return null;
                    }
                    
                    return new FileStream(filePath, FileMode.Open, FileAccess.Read);
                }
                else
                {
                    // Use S3 for production
                    if (_s3Client == null)
                        throw new InvalidOperationException("S3 client is not configured");
                        
                    var request = new GetObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = fileName
                    };

                    var response = await _s3Client.GetObjectAsync(request);
                    return response.ResponseStream;
                }
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File {FileName} not found in S3", fileName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file {FileName}", fileName);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            try
            {
                if (_useLocalStorage)
                {
                    // Use local file storage for development
                    var uploadPath = Path.Combine(_environment.WebRootPath, "uploads");
                    var filePath = Path.Combine(uploadPath, fileName);
                    
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        _logger.LogInformation("File {FileName} deleted from local storage successfully", fileName);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("File {FileName} not found in local storage", fileName);
                        return false;
                    }
                }
                else
                {
                    // Use S3 for production
                    if (_s3Client == null)
                        throw new InvalidOperationException("S3 client is not configured");
                        
                    var request = new DeleteObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = fileName
                    };

                    await _s3Client.DeleteObjectAsync(request);
                    _logger.LogInformation("File {FileName} deleted from S3 successfully", fileName);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileName}", fileName);
                return false;
            }
        }

        public async Task<string> GetFileUrlAsync(string fileName)
        {
            try
            {
                if (_useLocalStorage)
                {
                    // For local development, return a relative URL
                    return $"/api/images/file/{fileName}";
                }
                else
                {
                    // Use S3 for production
                    if (_s3Client == null)
                        throw new InvalidOperationException("S3 client is not configured");
                        
                    var request = new GetPreSignedUrlRequest
                    {
                        BucketName = _bucketName,
                        Key = fileName,
                        Expires = DateTime.UtcNow.AddHours(1), // URL expires in 1 hour
                        Verb = HttpVerb.GET
                    };

                    return await _s3Client.GetPreSignedURLAsync(request);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating URL for file {FileName}", fileName);
                throw;
            }
        }
    }
}
