using Amazon.S3.Model;

namespace PicBed.Services
{
    public interface IS3StorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string fileName);
        Task<Stream?> GetFileStreamAsync(string fileName);
        Task<bool> DeleteFileAsync(string fileName);
        Task<string> GetFileUrlAsync(string fileName);
    }
}
