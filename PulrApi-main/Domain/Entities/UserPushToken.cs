using Core.Domain.Common;
using System;

namespace Core.Domain.Entities
{
    public class UserPushToken : EntityBase
    {
        public int UserId { get; set; }
        public string ExpoToken { get; set; }
        public string DeviceId { get; set; }
    }
} 