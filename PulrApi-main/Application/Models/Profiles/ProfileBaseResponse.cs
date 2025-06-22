namespace Core.Application.Models.Profiles
{
    public class ProfileBaseResponse
    {
        public string Uid { get; set; }
        public string UserId { get; set; }
        public string ImageUrl { get; set; }
        public bool IsStore { get; set; }
        public bool IsInfluencer { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public bool FollowedByMe { get; set; }
    }
}
