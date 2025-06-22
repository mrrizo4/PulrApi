using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Core.Domain.Entities;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class TestController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<TestController> _logger;

        public TestController(UserManager<User> userManager, ILogger<TestController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("ping")]
        [AllowAnonymous]
        public IActionResult Ping()
        {
            _logger.LogInformation("Ping endpoint called at {Time}", DateTime.UtcNow);
            return Ok("Pong");
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword()
        {
            try
            {
                _logger.LogInformation("Reset password request for dule@pulr.com at {Time}", DateTime.UtcNow);
                var user = await _userManager.FindByEmailAsync("dule@pulr.com");
                if (user == null)
                {
                    _logger.LogWarning("User not found: dule@pulr.com");
                    return NotFound("User not found");
                }
                _logger.LogInformation("Found user: {UserId}", user.Id);

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                _logger.LogInformation("Generated password reset token");
                var result = await _userManager.ResetPasswordAsync(user, token, "T3stko$");
                if (result.Succeeded)
                {
                    _logger.LogInformation("Password reset successfully for dule@pulr.com");
                    return Ok("Password reset successfully");
                }

                _logger.LogError("Password reset failed: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset password: {Message}", ex.Message);
                return StatusCode(500, new { message = "Password reset failed", details = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}
