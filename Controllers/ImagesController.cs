using Microsoft.AspNetCore.Mvc;
using PicBed.Models;
using PicBed.Services;

namespace PicBed.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IImageService _imageService;
        private readonly IAuthService _authService;
        private readonly ILogger<ImagesController> _logger;

        public ImagesController(IImageService imageService, IAuthService authService, ILogger<ImagesController> logger)
        {
            _imageService = imageService;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Upload an image
        /// </summary>
        /// <param name="file">Image file to upload</param>
        /// <param name="description">Optional description</param>
        /// <param name="category">Optional category</param>
        /// <returns>Image information</returns>
        [HttpPost("upload")]
        public async Task<ActionResult<ImageRecord>> UploadImage(
            IFormFile file,
            [FromForm] string? description = null,
            [FromForm] string? category = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded");
                }

                // Get current user from token
                var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized("Authentication required");
                }

                var user = await _authService.GetUserByTokenAsync(token);
                if (user == null)
                {
                    return Unauthorized("Invalid token");
                }

                // Check upload limit for non-admin users
                if (user.Username != "admin")
                {
                    var currentImageCount = await _imageService.GetUserImageCountAsync(user.Id);
                    if (currentImageCount >= 1)
                    {
                        return BadRequest(new { 
                            message = "Upload limit reached. You can only upload 1 image. For more uploads, please contact xueshanchen1122@gmail.com to request additional permissions." 
                        });
                    }
                }

                var imageInfo = await _imageService.UploadImageAsync(file, user.Id, description, category);
                return Ok(imageInfo);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get image information by ID
        /// </summary>
        /// <param name="id">Image ID</param>
        /// <returns>Image information</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ImageRecord>> GetImageInfo(int id)
        {
            var imageInfo = await _imageService.GetImageInfoAsync(id);
            if (imageInfo == null)
            {
                return NotFound();
            }

            return Ok(imageInfo);
        }

        /// <summary>
        /// Get list of images with pagination
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <param name="category">Filter by category</param>
        /// <returns>List of images</returns>
        [HttpGet]
        public async Task<ActionResult<List<ImageRecord>>> GetImages(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? category = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var images = await _imageService.GetImagesAsync(page, pageSize, category);
            return Ok(images);
        }

        /// <summary>
        /// Delete an image by ID
        /// </summary>
        /// <param name="id">Image ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteImage(int id)
        {
            var success = await _imageService.DeleteImageAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// Get image file by filename
        /// </summary>
        /// <param name="fileName">Image filename</param>
        /// <returns>Image file</returns>
        [HttpGet("file/{fileName}")]
        public async Task<IActionResult> GetImageFile(string fileName)
        {
            var imageInfo = await _imageService.GetImageInfoByFileNameAsync(fileName);
            if (imageInfo == null)
            {
                return NotFound();
            }

            var stream = await _imageService.GetImageStreamAsync(fileName);
            if (stream == null)
            {
                return NotFound();
            }

            return File(stream, imageInfo.MimeType, imageInfo.OriginalFileName);
        }

        /// <summary>
        /// Get thumbnail by filename
        /// </summary>
        /// <param name="fileName">Image filename</param>
        /// <param name="width">Thumbnail width (default: 200)</param>
        /// <param name="height">Thumbnail height (default: 200)</param>
        /// <returns>Thumbnail image</returns>
        [HttpGet("thumbnail/{fileName}")]
        public async Task<IActionResult> GetThumbnail(string fileName, [FromQuery] int width = 200, [FromQuery] int height = 200)
        {
            var imageInfo = await _imageService.GetImageInfoByFileNameAsync(fileName);
            if (imageInfo == null)
            {
                return NotFound();
            }

            var stream = await _imageService.GetThumbnailStreamAsync(fileName, width, height);
            if (stream == null)
            {
                return NotFound();
            }

            return File(stream, "image/jpeg", $"thumb_{fileName}");
        }
    }
}
