using System;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;

        public NotificationController(
            INotificationService notificationService,
            ICurrentUserService currentUserService)
        {
            _notificationService = notificationService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            var currentUserId = int.Parse(_currentUserService.GetUserId());
            var notifications = await _notificationService.GetUserNotificationsAsync(currentUserId, skip, take);
            return Ok(notifications);
        }

        [HttpPost("like")]
        public async Task<IActionResult> SaveLikeNotification([FromBody] LikeNotificationRequest request)
        {
            var currentUserId = int.Parse(_currentUserService.GetUserId());
            await _notificationService.SaveLikeNotificationAsync(currentUserId, request.PostId);
            return Ok();
        }

        [HttpPost("mention")]
        public async Task<IActionResult> SaveMentionNotification([FromBody] MentionNotificationRequest request)
        {
            var currentUserId = int.Parse(_currentUserService.GetUserId());
            await _notificationService.SaveMentionNotificationAsync(
                currentUserId,
                request.MentionedUserId,
                request.TargetId,
                request.MentionType);
            return Ok();
        }

        [HttpDelete("{notificationId}")]
        public async Task<IActionResult> DeleteNotification(int notificationId)
        {
            await _notificationService.DeleteNotificationAsync(notificationId);
            return Ok();
        }

        [HttpPut("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            await _notificationService.MarkNotificationAsReadAsync(notificationId);
            return Ok();
        }

        // Push token management endpoints
        [HttpPost("push-token")]
        public async Task<IActionResult> SavePushToken([FromBody] PushTokenRequest request)
        {
            var currentUserId = int.Parse(_currentUserService.GetUserId());
            await _notificationService.SavePushTokenAsync(currentUserId, request.ExpoToken, request.DeviceId);
            return Ok();
        }

        [HttpDelete("push-token/{deviceId}")]
        public async Task<IActionResult> DeletePushToken(string deviceId)
        {
            var currentUserId = int.Parse(_currentUserService.GetUserId());
            await _notificationService.DeletePushTokenAsync(currentUserId, deviceId);
            return Ok();
        }

        [HttpGet("push-tokens")]
        public async Task<IActionResult> GetPushTokens()
        {
            var currentUserId = int.Parse(_currentUserService.GetUserId());
            var tokens = await _notificationService.GetUserPushTokensAsync(currentUserId);
            return Ok(tokens);
        }
    }

    public class LikeNotificationRequest
    {
        public int PostId { get; set; }
    }

    public class MentionNotificationRequest
    {
        public int MentionedUserId { get; set; }
        public int TargetId { get; set; }
        public string MentionType { get; set; } // "Post" or "Comment"
    }

    public class PushTokenRequest
    {
        public string ExpoToken { get; set; }
        public string DeviceId { get; set; }
    }
} 