using PicBed.Models;

namespace PicBed.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<bool> ValidateTokenAsync(string token);
        Task<User?> GetUserByTokenAsync(string token);
        string GenerateToken(User user);
    }
}
