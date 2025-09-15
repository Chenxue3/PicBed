using Microsoft.AspNetCore.Mvc;
using PicBed.Models;
using PicBed.Services;

namespace PicBed.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// User login
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>Login response with token</returns>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Validate token
        /// </summary>
        /// <returns>Token validation result</returns>
        [HttpGet("validate")]
        public async Task<ActionResult> ValidateToken()
        {
            try
            {
                var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Token is required" });
                }

                var isValid = await _authService.ValidateTokenAsync(token);
                
                if (!isValid)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                return Ok(new { message = "Token is valid" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
