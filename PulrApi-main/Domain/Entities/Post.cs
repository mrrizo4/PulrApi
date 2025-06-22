using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class Post : EntityBase
    {
        public string Text { get; set; }

        [Required]
        public User User { get; set; }

        public Store Store { get; set; }

        public MediaFile MediaFile { get; set; }
        public Post SharedPost { get; set; }

        public virtual ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();
        public virtual ICollection<PostHashtag> PostHashtags { get; set; } = new List<PostHashtag>();
        public virtual ICollection<PostProfileMention> PostProfileMentions { get; set; } = new List<PostProfileMention>();
        public virtual ICollection<PostStoreMention> PostStoreMentions { get; set; } = new List<PostStoreMention>();
        public virtual ICollection<PostProductTag> PostProductTags { get; set; } = new List<PostProductTag>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
        public virtual ICollection<PostClick> PostClicks { get; set; } = new List<PostClick>();
        public virtual ICollection<PostMyStyle> PostMyStyles { get; set; } = new List<PostMyStyle>();
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}