using PicBed.Models;

namespace PicBed.Services
{
    public interface IImageService
    {
        Task<ImageRecord> UploadImageAsync(IFormFile file, int userId, string? description = null, string? category = null);
        Task<ImageRecord?> GetImageInfoAsync(int id);
        Task<ImageRecord?> GetImageInfoByFileNameAsync(string fileName);
        Task<List<ImageRecord>> GetImagesAsync(int page = 1, int pageSize = 20, string? category = null);
        Task<bool> DeleteImageAsync(int id);
        Task<Stream?> GetImageStreamAsync(string fileName);
        Task<Stream?> GetThumbnailStreamAsync(string fileName, int width = 200, int height = 200);
        Task<int> GetUserImageCountAsync(int userId);
    }
}
