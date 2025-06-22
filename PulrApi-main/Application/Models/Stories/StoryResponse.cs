using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using System;
using System.Collections.Generic;

namespace Core.Application.Models.Stories
{
    public class StoryResponse
    {
        public string Uid { get; set; }
        public string Text { get; set; }
        public string DisplayName { get; set; }
        public int LikesCount { get; set; }
        public bool LikedByMe { get; set; }
        public bool SeenByMe { get; set; }
        public MediaFileDetailsResponse MediaFile { get; set; }
        public IEnumerable<ProductTagCoordinatesResponse> TaggedProducts { get; set; }
        public bool PostedByStore { get; set; }
        public int CommentsCount { get; set; }
        public DateTime CreatedAt { get; internal set; }
        public string EntityUid { get; internal set; }
    }
}
