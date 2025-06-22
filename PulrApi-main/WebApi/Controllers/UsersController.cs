using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Mediatr.Users.Commands.Delete;
using Core.Application.Mediatr.Users.Commands.Login;
using Core.Application.Mediatr.Users.Commands.Password;
using Core.Application.Mediatr.Users.Commands.Register;
using Core.Application.Mediatr.Users.Queries;
using Core.Application.Models.Users;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using System;
using System.Collections.Generic;
using Core.Application.Exceptions;
using Core.Application.Mediatr.Users.Commands;
using System.Linq;
using Core.Application.Helpers;
using Microsoft.AspNetCore.Identity;
using Core.Domain.Entities;
using Core.Application.Mediatr.Users.Commands.Deactivate;

namespace WebApi.Controllers
{
    public class UsersController : ApiControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UsersController> _logger;
        private readonly IUserService _userService;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly UserManager<User> _userManager;

        public UsersController(
            IConfiguration configuration, 
            ILogger<UsersController> logger,
            IUserService userService,
            ITokenBlacklistService tokenBlacklistService,
            UserManager<User> userManager
            )
        {
            _configuration = configuration;
            _logger = logger;
            _userService = userService;
            _tokenBlacklistService = tokenBlacklistService;
            _userManager = userManager;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginCommand command)
        {
            var res = await Mediator.Send(command);
            if (!string.IsNullOrEmpty(res.Token))
            {
                await _userService.SaveLoginActivityAsync(
                    res.Id,
                    command.Device.Brand,
                    command.Device.ModelName,
                    command.Device.OsVersion,
                    command.Device.DeviceIdentifier,
                    "Logged in"
                );
            }
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpPost("login-facebook")]
        public async Task<ActionResult<LoginResponse>> FacebookLogin(FacebookLoginCommand command)
        {
            var res = await Mediator.Send(command);
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpPost("login-google-token")]
        public async Task<ActionResult<LoginResponse>> GoogleLoginWithToken(GoogleLoginCommand command)
        {
            var res = await Mediator.Send(command);
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpPost("login-apple")]
        public async Task<ActionResult<LoginResponse>> AppleLogin(AppleLoginCommand command)
        {
            var res = await Mediator.Send(command);
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpGet("debug-google-config")]
        public IActionResult DebugGoogleConfig()
        {
            var config = new
            {
                ClientId = _configuration["GoogleAuth:ClientId"],
                ClientSecret = _configuration["GoogleAuth:ClientSecret"],
                IsClientIdEmpty = string.IsNullOrEmpty(_configuration["GoogleAuth:ClientId"]),
                IsClientSecretEmpty = string.IsNullOrEmpty(_configuration["GoogleAuth:ClientSecret"])
            };

            return Ok(config);
        }

        [HttpGet("data")]
        public async Task<ActionResult<LoginResponse>> GetCurrentUserDataQuery()
        {
            var res = await Mediator.Send(new GetCurrentUserDataQuery());
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<ActionResult> PasswordResetRequest([FromBody] PasswordResetRequestCommand command)
        {
            await Mediator.Send(command);
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("verify-otp")]
        public async Task<ActionResult<bool>> VerifyOtp([FromBody] VerifyOtpCommand command)
        {
            try
            {
                var result = await Mediator.Send(command);
                return Ok(result);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP");
                return StatusCode(500, new { message = "An error occurred while verifying the OTP" });
            }
        }

        [HttpPost("send-email-verification-otp")]
        [AllowAnonymous]
        public async Task<ActionResult<EmailVerificationResponse>> SendEmailVerificationOtp([FromBody] SendEmailVerificationOtpCommand command)
        {
            try
            {
                var result = await Mediator.Send(command);
                return Ok(result);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email verification OTP");
                return StatusCode(500, new { Message = "An error occurred while sending the verification OTP." });
            }
        }

        [AllowAnonymous]
        [HttpPost("change-password-from-email")]
        public async Task<ActionResult> ChangePasswordFromEmail([FromBody] ChangePasswordFromEmailCommand command)
        {
            await Mediator.Send(command);
            return Ok();
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
        {
            await Mediator.Send(command);
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterCommand command)
        {
            await Mediator.Send(command);
            return Ok();
        }

        [AllowAnonymous]
        [HttpGet("confirm-email")]
        public async Task<ActionResult> ConfirmEmail([FromQuery] ConfirmEmailCommand command)
        {
            try
            {
                await Mediator.Send(command);
                return Ok(new { message = "Email confirmed successfully" });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email");
                return StatusCode(500, new { message = "An error occurred while confirming your email" });
            }
        }

        [AllowAnonymous]
        [HttpPost("check-username")]
        public async Task<ActionResult<CheckUsernameResponse>> CheckUsername([FromBody] CheckUsernameRequest request)
        {
            var normalization = UsernameHelper.Normalize(request.Username);
            var User = await _userManager.FindByNameAsync(normalization);

            return Ok(new CheckUsernameResponse
            {
                Exists = User != null,
                Message = User != null ? "Username already exists" : "Username is available"
            });
        }

        [HttpDelete]
        public async Task<ActionResult> Delete()
        {
            await Mediator.Send(new DeleteUserCommand());
            return NoContent();
        }

        [AllowAnonymous]
        [HttpGet("check-email")]
        public async Task<ActionResult<CheckEmailResponse>> CheckEmail([FromQuery] string email)
        {
            var response = await Mediator.Send(new CheckEmailQuery { Email = email });
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("accept-terms")]
        public async Task<ActionResult<bool>> AcceptTerms([FromBody] AcceptTermsCommand command)
        {
            try
            {
                var result = await Mediator.Send(command);
                return Ok(result);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting terms");
                return StatusCode(500, new { message = "An error occurred while accepting terms" });
            }
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPost("block")]
        public async Task<ActionResult<BlockUserResponse>> BlockUser([FromBody] BlockUserCommand command)
        {
            var response = await Mediator.Send(command);
            return Ok(response);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPost("unblock")]
        public async Task<ActionResult<UnblockUserResponse>> UnblockUser([FromBody] UnblockUserCommand command)
        {
            var response = await Mediator.Send(command);
            return Ok(response);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            
            if (token != null)
            {
                await _tokenBlacklistService.RemoveTokenAsync(token);
            }

            return Ok(new { message = "Successfully logged out" });
        }

        [HttpPost("deactivate")]
        [Authorize(Roles = PulrRoles.User)]
        public async Task<ActionResult> DeactivateAccount()
        {
            await Mediator.Send(new DeactivateUserCommand());
            return Ok(new { message = "Your account has been deactivated. You can reactivate it by logging back in." });
        }

        [Authorize]
        [HttpGet("login-activity")]
        public async Task<ActionResult<List<LoginActivityDto>>> GetLoginActivity()
        {
            var activities = await _userService.GetLoginActivityAsync();
            return Ok(activities);
        }

        [Authorize]
        [HttpGet("recognised-devices")]
        public async Task<ActionResult<List<RecognisedDeviceDto>>> GetRecognisedDevices()
        {
            var devices = await _userService.GetRecognisedDevicesAsync();
            return Ok(devices);
        }

        [Authorize]
        [HttpPost("signout-device")]
        public async Task<ActionResult> SignOutDevice([FromBody] SignOutDeviceRequest request)
        {
            await _userService.SignOutDeviceAsync(request.ActivityId);
            return Ok();
        }

        [Authorize]
        [HttpPost("signout-all-devices")]
        public async Task<ActionResult> SignOutAllDevices()
        {
            await _userService.SignOutAllDevicesAsync();
            return Ok();
        }

        [Authorize]
        [HttpGet("notification-settings")]
        public async Task<ActionResult<UserNotificationSettingDto>> GetNotificationSettings()
        {
            var settings = await _userService.GetNotificationSettingsAsync();
            return Ok(settings);
        }

        [Authorize]
        [HttpPut("notification-settings")]
        public async Task<ActionResult> UpdateNotificationSettings([FromBody] UserNotificationSettingDto dto)
        {
            await _userService.UpdateNotificationSettingsAsync(dto);
            return Ok();
        }

        //[Authorize]
        //[HttpGet("blocked-users")]
        //public async Task<ActionResult<List<BlockedUserDto>>> GetBlockedUsers()
        //{
        //    try
        //    {
        //        var blockedUsers = await Mediator.Send(new GetBlockedUsersQuery());
        //        return Ok(blockedUsers);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting blocked users");
        //        return StatusCode(500, new { Message = "An error occurred while getting blocked users." });
        //    }
        //}
    }
}
