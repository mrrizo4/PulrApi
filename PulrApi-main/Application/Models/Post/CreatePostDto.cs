using System.Collections.Generic;
using Core.Application.Models.Post;

namespace Core.Application.Models.Post
{
    public class CreatePostDto
    {
        public string StoreUid { get; set; }
        public string Text { get; set; }
        public List<string> Hashtags { get; set; } = new List<string>();
        public List<string> Mentions { get; set; } = new List<string>();
        public double SpotExpiryHours { get; set; } = 0;
        public List<PostProductTagDto> PostProductTags { get; set; } = new List<PostProductTagDto>();
    }
}
