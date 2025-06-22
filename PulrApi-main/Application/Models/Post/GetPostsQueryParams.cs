﻿using Core.Domain.Enums;

namespace Core.Application.Models.Post
{
    public class GetPostsQueryParams : PagingParamsRequest
    {
        public string EntityUid { get; set; }
        public ProfileTypeEnum ProfileType { get; set; } = ProfileTypeEnum.All;
        public PostTypeEnum PostType { get; set; } = PostTypeEnum.All;
        public PostSortingLogicEnum SortingLogic { get; set; } = PostSortingLogicEnum.Newest;
        public string CurrencyCode { get; set; }
        public string Categories { get; set; }
        public string Tags { get; set; }
        public string Hashtag { get; set; }
    }
}
