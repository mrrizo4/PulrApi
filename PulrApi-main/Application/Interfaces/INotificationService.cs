using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Domain.Entities;

namespace Core.Application.Interfaces
{
    public interface INotificationService
    {
        Task SaveLikeNotificationAsync(int likerUserId, int postId);
        Task SaveMentionNotificationAsync(int mentionedByUserId, int mentionedUserId, int targetId, string mentionType);
        Task DeleteNotificationAsync(int notificationId);
        Task MarkNotificationAsReadAsync(int notificationId);
        Task<List<NotificationHistory>> GetUserNotificationsAsync(int userId, int skip = 0, int take = 20);
        
        // Push token management
        Task SavePushTokenAsync(int userId, string expoToken, string deviceId);
        Task DeletePushTokenAsync(int userId, string deviceId);
        Task<List<UserPushToken>> GetUserPushTokensAsync(int userId);
    }
} 