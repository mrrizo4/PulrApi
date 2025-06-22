using Core.Domain.Common;
using Core.Domain.Enums;
using System;

namespace Core.Domain.Entities
{
    public class Activity : EntityBase
    {
        public int UserId { get; set; }
        public ActivityActionTypeEnum ActionType { get; set; }
        public int TargetId { get; set; } // Can be PostId, CommentId etc.
        public EntityTypeEnum TargetType { get; set; } // Post, Comment etc.
    }
} 