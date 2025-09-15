using Microsoft.EntityFrameworkCore;
using PicBed.Data;
using PicBed.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace PicBed.Services
{
    public class ImageService : IImageService
    {
        private readonly PicBedDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageService> _logger;
        private readonly IS3StorageService _s3StorageService;

        public ImageService(
            PicBedDbContext context,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<ImageService> logger,
            IS3StorageService s3StorageService)
        {
            _context = context;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
            _s3StorageService = s3StorageService;
        }

        public async Task<ImageRecord> UploadImageAsync(IFormFile file, int userId, string? description = null, string? category = null)
        {
            // Validate file
            var maxFileSize = _configuration.GetValue<long>("ImageSettings:MaxFileSize", 10485760); // 10MB
            var allowedExtensions = _configuration.GetSection("ImageSettings:AllowedExtensions").Get<string[]>() ?? 
                new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            if (file.Length > maxFileSize)
            {
                throw new ArgumentException($"File size exceeds maximum allowed size of {maxFileSize} bytes");
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException($"File extension {fileExtension} is not allowed");
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var thumbnailFileName = $"thumb_{fileName}";

            // Get image dimensions and create thumbnail
            int width = 0, height = 0;
            try
            {
                using (var imageStream = file.OpenReadStream())
                using (var image = await Image.LoadAsync(imageStream))
                {
                    width = image.Width;
                    height = image.Height;

                    // Create thumbnail in memory
                    using (var thumbnail = image.Clone(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(200, 200),
                        Mode = ResizeMode.Max
                    })))
                    using (var thumbnailStream = new MemoryStream())
                    {
                        await thumbnail.SaveAsync(thumbnailStream, image.Metadata.DecodedImageFormat!);
                        thumbnailStream.Position = 0;

                        // Upload thumbnail to S3
                        var thumbnailFormFile = new FormFile(thumbnailStream, 0, thumbnailStream.Length, "thumbnail", thumbnailFileName)
                        {
                            Headers = new HeaderDictionary(),
                            ContentType = file.ContentType
                        };
                        await _s3StorageService.UploadFileAsync(thumbnailFormFile, thumbnailFileName);
                    }
                }

                // Upload original file to S3
                await _s3StorageService.UploadFileAsync(file, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image {FileName}", fileName);
                throw new InvalidOperationException("Invalid image file", ex);
            }

            // Save to database
            var imageInfo = new ImageRecord
            {
                FileName = fileName,
                OriginalFileName = file.FileName,
                FileExtension = fileExtension,
                FileSize = file.Length,
                Width = width,
                Height = height,
                MimeType = file.ContentType,
                Description = description,
                Category = category,
                UploadTime = DateTime.UtcNow,
                UserId = userId
            };

            _context.Images.Add(imageInfo);
            await _context.SaveChangesAsync();

            return imageInfo;
        }

        public async Task<ImageRecord?> GetImageInfoAsync(int id)
        {
            return await _context.Images.FindAsync(id);
        }

        public async Task<ImageRecord?> GetImageInfoByFileNameAsync(string fileName)
        {
            return await _context.Images.FirstOrDefaultAsync(i => i.FileName == fileName);
        }

        public async Task<List<ImageRecord>> GetImagesAsync(int page = 1, int pageSize = 20, string? category = null)
        {
            var query = _context.Images.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(i => i.Category == category);
            }

            return await query
                .OrderByDescending(i => i.UploadTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> DeleteImageAsync(int id)
        {
            var imageInfo = await _context.Images.FindAsync(id);
            if (imageInfo == null)
            {
                return false;
            }

            // Delete physical files
            var uploadPath = Path.Combine(_environment.WebRootPath, _configuration["ImageSettings:UploadPath"] ?? "uploads");
            var thumbnailPath = Path.Combine(_environment.WebRootPath, _configuration["ImageSettings:ThumbnailPath"] ?? "thumbnails");

            var filePath = Path.Combine(uploadPath, imageInfo.FileName);
            var thumbnailFilePath = Path.Combine(thumbnailPath, $"thumb_{imageInfo.FileName}");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            if (File.Exists(thumbnailFilePath))
            {
                File.Delete(thumbnailFilePath);
            }

            // Delete from database
            _context.Images.Remove(imageInfo);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<Stream?> GetImageStreamAsync(string fileName)
        {
            return await _s3StorageService.GetFileStreamAsync(fileName);
        }

        public async Task<Stream?> GetThumbnailStreamAsync(string fileName, int width = 200, int height = 200)
        {
            var thumbnailFileName = $"thumb_{fileName}";
            return await _s3StorageService.GetFileStreamAsync(thumbnailFileName);
        }

        public async Task<int> GetUserImageCountAsync(int userId)
        {
            return await _context.Images.CountAsync(i => i.UserId == userId);
        }
    }
}
