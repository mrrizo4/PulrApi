using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IApplicationDbContext _context;

        public NotificationService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SaveLikeNotificationAsync(int likerUserId, int postId)
        {
            // Get post owner
            var post = await _context.Posts
                .Include(p => p.User.Profile)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null || post.User.Profile == null)
                throw new ArgumentException("Post not found");

            // Don't create notification if user likes their own post
            if (post.User.Profile.Id == likerUserId)
                return;

            // Save activity
            var activity = new Activity
            {
                UserId = likerUserId,
                ActionType = ActivityActionTypeEnum.LikePost,
                TargetId = postId,
                TargetType = EntityTypeEnum.POST
            };
            _context.Activities.Add(activity);

            // Save notification
            var notification = new NotificationHistory
            {
                ReceiverUserId = post.User.Profile.Id,
                ActorUserId = likerUserId,
                ActionType = NotificationActionTypeEnum.Like,
                TargetId = postId,
                IsRead = false
            };
            _context.NotificationHistories.Add(notification);

            await _context.SaveChangesAsync(cancellationToken: default);
        }

        public async Task SaveMentionNotificationAsync(int mentionedByUserId, int mentionedUserId, int targetId, string mentionType)
        {
            // Save mention
            var mention = new Mention
            {
                MentionedUserId = mentionedUserId,
                MentionedByUserId = mentionedByUserId,
                TargetId = targetId,
                MentionType = mentionType == "Post" ? EntityTypeEnum.POST : EntityTypeEnum.COMMENT
            };
            _context.Mentions.Add(mention);

            // Save activity
            var activity = new Activity
            {
                UserId = mentionedByUserId,
                ActionType = mentionType == "Post" ? ActivityActionTypeEnum.MentionPost : ActivityActionTypeEnum.MentionComment,
                TargetId = targetId,
                TargetType = mentionType == "Post" ? EntityTypeEnum.POST : EntityTypeEnum.COMMENT
            };
            _context.Activities.Add(activity);

            // Save notification
            var notification = new NotificationHistory
            {
                ReceiverUserId = mentionedUserId,
                ActorUserId = mentionedByUserId,
                ActionType = NotificationActionTypeEnum.Mention,
                TargetId = targetId,
                IsRead = false
            };
            _context.NotificationHistories.Add(notification);

            await _context.SaveChangesAsync(cancellationToken: default);
        }

        public async Task DeleteNotificationAsync(int notificationId)
        {
            var notification = await _context.NotificationHistories
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification != null)
            {
                _context.NotificationHistories.Remove(notification);
                await _context.SaveChangesAsync(cancellationToken: default);
            }
        }

        public async Task MarkNotificationAsReadAsync(int notificationId)
        {
            var notification = await _context.NotificationHistories
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync(cancellationToken: default);
            }
        }

        public async Task<List<NotificationHistory>> GetUserNotificationsAsync(int userId, int skip = 0, int take = 20)
        {
            return await _context.NotificationHistories
                .Where(n => n.ReceiverUserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task SavePushTokenAsync(int userId, string expoToken, string deviceId)
        {
            // Check if token already exists for this device
            var existingToken = await _context.UserPushTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceId == deviceId);

            if (existingToken != null)
            {
                // Update existing token
                existingToken.ExpoToken = expoToken;
                existingToken.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new token
                var newToken = new UserPushToken
                {
                    UserId = userId,
                    ExpoToken = expoToken,
                    DeviceId = deviceId
                };
                _context.UserPushTokens.Add(newToken);
            }

            await _context.SaveChangesAsync(cancellationToken: default);
        }

        public async Task DeletePushTokenAsync(int userId, string deviceId)
        {
            var token = await _context.UserPushTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceId == deviceId);

            if (token != null)
            {
                _context.UserPushTokens.Remove(token);
                await _context.SaveChangesAsync(cancellationToken: default);
            }
        }

        public async Task<List<UserPushToken>> GetUserPushTokensAsync(int userId)
        {
            return await _context.UserPushTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }
    }
} 