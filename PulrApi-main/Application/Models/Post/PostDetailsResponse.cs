using System;
using System.Collections.Generic;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Post;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Domain.Enums;

namespace Core.Application.Models.Post
{
    public class PostDetailsResponse
    {
        internal bool PostedByStore;

        public string Text { get; set; }
        public MediaFileDetailsResponse MediaFile { get; set; }
        public virtual ICollection<string> PostHashtags { get; set; }
        public virtual ICollection<string> PostProfileMentions { get; set; }
        public virtual ICollection<string> PostStoreMentions { get; set; }
        public virtual ICollection<PostProductTagResponse> PostProductTags { get; set; }
        public ProfileDetailsResponse Profile { get; set; }
        public StoreDetailsResponse Store { get; set; }
        public DateTime CreatedAt { get; set; }
        public int LikesCount { get; set; }
        public bool LikedByMe { get; set; }
        public string Uid { get; set; }
        public bool IsMyStyle { get; set; }
        public List<string> TaggedProductUids { get; internal set; }
        public string ProfileUid { get; internal set; }
        public string StoreUid { get; internal set; }
        public PostTypeEnum PostType { get; internal set; }
        public int CommentsCount { get; internal set; }
        // public int TotalParentCommentsCount { get; internal set; }
        // public int RepliesCount { get; internal set; }
        public int BookmarksCount { get; internal set; }
        public int MyStylesCount { get; internal set; }
        public bool BookmarkedByMe { get; internal set; }
    }
}
