namespace Core.Application.Models.Users
{
    public class UserNotificationSettingDto
    {
        public bool? Likes { get; set; }
        public bool? Comments { get; set; }
        public bool? Mentions { get; set; }
        public bool? Follows { get; set; }
        public bool? SavedPosts { get; set; }
        public bool? ShopActivity { get; set; }
        public bool? DirectMessages { get; set; }
        public bool? EmailNotification { get; set; }
    }
} 