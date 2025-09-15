using PicBed.Services;

namespace PicBed.Middleware
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthMiddleware> _logger;

        public AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuthService authService)
        {
            // Skip authentication for certain paths
            var path = context.Request.Path.Value?.ToLower();
            if (ShouldSkipAuth(path))
            {
                await _next(context);
                return;
            }

            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            
            if (string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Token is required");
                return;
            }

            var isValid = await authService.ValidateTokenAsync(token);
            if (!isValid)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Invalid token");
                return;
            }

            await _next(context);
        }

        private static bool ShouldSkipAuth(string? path)
        {
            if (string.IsNullOrEmpty(path)) return true;

            var skipPaths = new[]
            {
                "/api/auth/login",
                "/api/auth/register",
                "/api/auth/validate",
                "/swagger",
                "/index.html",
                "/",
                "/api/images",
                "/api/images/file/",
                "/api/images/thumbnail/"
            };

            return skipPaths.Any(skipPath => path.StartsWith(skipPath));
        }
    }
}
