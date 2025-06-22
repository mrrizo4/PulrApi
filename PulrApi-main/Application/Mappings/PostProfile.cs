using System.Linq;
using System.Collections.Generic;
using Core.Application.Mediatr.Posts.Commands;
using Core.Application.Mediatr.Posts.Queries;
using Core.Application.Models;
using Core.Application.Models.Post;
using Core.Domain.Entities;
using Core.Application.Models.Profiles;

namespace Core.Application.Mappings
{
    public class PostProfile : AutoMapper.Profile
    {
        public PostProfile()
        {
            // TODO split to 2 endpoints
            CreateMap<GetPostsQuery, GetPostsQueryParams>();

            CreateMap<CreatePostCommand, CreatePostDto>()
                .ForMember(dest => dest.Hashtags, opt => opt.MapFrom(src => src.Hashtags ?? new List<string>()));
            CreateMap<SharePostCommand, SharePostDto>();

            //CreateMap<AddMediaFileToPostCommand, PostMediaFileAddDto>();

            CreateMap<PostProductTag, PostProductTagResponse>();

            CreateMap<Post, PostDetailsResponse>()
                .ForMember(dest => dest.PostProfileMentions, opt => opt.MapFrom(src => src.PostProfileMentions.Select(e => e.Profile.User.UserName)))
                .ForMember(dest => dest.PostStoreMentions, opt => opt.MapFrom(src => src.PostStoreMentions.Select(e => e.Store.UniqueName)))
                .ForMember(dest => dest.PostHashtags, opt => opt.MapFrom(src => src.PostHashtags.Select(h => h.Hashtag.Value)))
                .ForMember(dest => dest.Profile, opt => opt.MapFrom(src => new ProfileDetailsResponse
                {
                    Uid = src.User.Profile.Uid,
                    //UserId = src.User.Id,
                    ImageUrl = src.User.Profile.ImageUrl,
                    IsStore = false,
                    //IsInfluencer = src.User.Profile.IsInfluencer,
                    FullName  = src.User.FirstName,
                    FirstName = src.User.FirstName,
                    LastName = src.User.LastName,
                    Username = src.User.UserName,
                    DisplayName = src.User.DisplayName,
                    FollowedByMe = false
                }));

            CreateMap<Post, PostResponse>()
                .ForMember(dest => dest.PostProfileMentions, opt => opt.MapFrom(src => src.PostProfileMentions.Select(e => e.Profile.User.UserName)))
                .ForMember(dest => dest.PostStoreMentions, opt => opt.MapFrom(src => src.PostStoreMentions.Select(e => e.Store.UniqueName)))
                .ForMember(dest => dest.PostHashtags, opt => opt.MapFrom(src => src.PostHashtags.Select(h => h.Hashtag.Value)))
                .ForMember(dest => dest.Profile, opt => opt.MapFrom(src => new ProfileBaseResponse
                {
                    Uid = src.User.Profile.Uid,
                    UserId = src.User.Id,
                    ImageUrl = src.User.Profile.ImageUrl,
                    IsStore = false,
                    //IsInfluencer = src.User.Profile.IsInfluencer,
                    FullName = src.User.FirstName,
                    FirstName = src.User.FirstName,
                    LastName = src.User.LastName,
                    Username = src.User.UserName,
                    DisplayName = src.User.DisplayName,
                    FollowedByMe = false
                }));

            CreateMap<PagedList<Post>, PagingResponse<PostResponse>>().ForMember(
                            dest => dest.Items, opt => opt.MapFrom(src => src));

            CreateMap<PagedList<PostResponse>, PagingResponse<PostResponse>>().ForMember(
                            dest => dest.Items, opt => opt.MapFrom(src => src));
        }
    }
}
