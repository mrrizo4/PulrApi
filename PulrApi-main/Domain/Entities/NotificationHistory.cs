using Core.Domain.Common;
using Core.Domain.Enums;
using System;

namespace Core.Domain.Entities
{
    public class NotificationHistory : EntityBase
    {
        public int ReceiverUserId { get; set; }
        public int ActorUserId { get; set; }
        public NotificationActionTypeEnum ActionType { get; set; }
        public int TargetId { get; set; }
        public bool IsRead { get; set; } = false;
    }
} 