using Microsoft.EntityFrameworkCore;
using PicBed.Data;
using PicBed.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PicBed.Services
{
    public class AuthService : IAuthService
    {
        private readonly PicBedDbContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly string _secretKey = "PicBed-Secret-Key-2024"; // In production, use proper secret management

        public AuthService(PicBedDbContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

                if (user == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    };
                }

                var passwordHash = HashPassword(request.Password);
                if (user.PasswordHash != passwordHash)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    };
                }

                // Update last login time
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var token = GenerateToken(user);

                return new LoginResponse
                {
                    Success = true,
                    Token = token,
                    User = user,
                    Message = "Login successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Username}", request.Username);
                return new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred during login"
                };
            }
        }

        public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Check if username already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username);
                
                if (existingUser != null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Username already exists"
                    };
                }

                // Check if email already exists
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);
                
                if (existingEmail != null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Email already exists"
                    };
                }

                // Create new user
                var newUser = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                var token = GenerateToken(newUser);

                return new LoginResponse
                {
                    Success = true,
                    Token = token,
                    User = newUser,
                    Message = "Registration successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user {Username}", request.Username);
                return new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred during registration"
                };
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var user = await GetUserByTokenAsync(token);
                return user != null && user.IsActive;
            }
            catch
            {
                return false;
            }
        }

        public async Task<User?> GetUserByTokenAsync(string token)
        {
            try
            {
                var tokenData = DecodeToken(token);
                if (tokenData == null) return null;

                var userIdObj = tokenData.GetValueOrDefault("userId", 0);
                if (userIdObj is not int userId || userId == 0) return null;

                return await _context.Users.FindAsync(userId);
            }
            catch
            {
                return null;
            }
        }

        public string GenerateToken(User user)
        {
            var tokenData = new
            {
                userId = user.Id,
                username = user.Username,
                issuedAt = DateTime.UtcNow.Ticks,
                expiresAt = DateTime.UtcNow.AddDays(7).Ticks
            };

            var json = JsonSerializer.Serialize(tokenData);
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            var signature = ComputeSignature(encoded);

            return $"{encoded}.{signature}";
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + _secretKey));
            return Convert.ToBase64String(hashedBytes);
        }

        private string ComputeSignature(string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
            var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(signature);
        }

        private Dictionary<string, object>? DecodeToken(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length != 2) return null;

                var encoded = parts[0];
                var signature = parts[1];

                // Verify signature
                var expectedSignature = ComputeSignature(encoded);
                if (signature != expectedSignature) return null;

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                // Check expiration
                if (tokenData != null && tokenData.ContainsKey("expiresAt"))
                {
                    var expiresAt = Convert.ToInt64(tokenData["expiresAt"]);
                    if (DateTime.UtcNow.Ticks > expiresAt)
                    {
                        return null;
                    }
                }

                return tokenData;
            }
            catch
            {
                return null;
            }
        }
    }
}
