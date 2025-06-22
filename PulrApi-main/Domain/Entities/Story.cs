using System;
using System.Collections.Generic;

namespace Core.Domain.Entities;

public class Story : EntityBase
{
    public string Text { get; set; }
    public DateTime StoryExpiresIn { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; }
    public User User { get; set; }
    public int? StoreId { get; set; }
    public Store Store { get; set; }
    public int? SharedPostId { get; set; }
    public Post SharedPost { get; set; }
    public MediaFile MediaFile { get; set; }
    public ICollection<StoryLike> StoryLikes { get; set; } = new List<StoryLike>();
    public virtual ICollection<StoryHashTag> StoryHashTags { get; set; } = new List<StoryHashTag>();
    public virtual ICollection<StoryProductTag> StoryProductTags { get; set; } = new List<StoryProductTag>();
    public virtual ICollection<StoryProfileMention> StoryProfileMentions { get; set; } = new List<StoryProfileMention>();
    public virtual ICollection<StorySeen> StorySeens { get; set; } = new List<StorySeen>();

}