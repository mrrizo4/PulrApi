using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Profiles.Queries
{
    public class GetProfileFollowersQuery : PagingParamsRequest, IRequest<PagingResponse<ProfileDetailsResponse>>
    {
        [Required]
        public string ProfileUid { get; set; }
    }

    public class GetProfileFollowersQueryHandler : IRequestHandler<GetProfileFollowersQuery, PagingResponse<ProfileDetailsResponse>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetProfileFollowersQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetProfileFollowersQueryHandler(IApplicationDbContext dbContext, 
            ILogger<GetProfileFollowersQueryHandler> logger, 
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<PagingResponse<ProfileDetailsResponse>> Handle(GetProfileFollowersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();

                var profile = await _dbContext.Profiles.Where(p => p.Uid == request.ProfileUid).SingleOrDefaultAsync(cancellationToken);
                if (profile == null)
                {
                    throw new BadRequestException("Profile doesnt exist");
                }

                IQueryable<ProfileFollower> profileFollowersQueryable = _dbContext.ProfileFollowers;

                var profileFollowers = profileFollowersQueryable.Where(pf => pf.ProfileId == profile.Id && pf.Follower.IsActive)
                    .Select(p => new ProfileDetailsResponse
                    {
                        Uid = p.Follower.Uid,
                        FullName = p.Follower.User.FirstName,
                        FirstName = p.Follower.User.FirstName,
                        LastName = p.Follower.User.LastName,
                        Username = p.Follower.User.UserName,
                        ImageUrl = p.Follower.ImageUrl,
                        Followers = p.Follower.ProfileFollowers.Count(),
                        Following = p.Follower.ProfileFollowings.Count(),
                        PostsCount = p.Follower.User.Posts.Count(),
                        About = p.Follower.About,
                        Location = p.Follower.Location,
                        WebsiteUrl = p.Follower.ProfileSocialMedia.WebsiteUrl,
                        InstagramUrl = p.Follower.ProfileSocialMedia.InstagramUrl,
                        FacebookUrl = p.Follower.ProfileSocialMedia.FacebookUrl,
                        TwitterUrl = p.Follower.ProfileSocialMedia.TwitterUrl,
                        TikTokUrl = p.Follower.ProfileSocialMedia.TikTokUrl,
                        ActiveStoriesCount = p.Follower.User.Stories.Count,
                        FollowedByMe = cUser != null ? profileFollowersQueryable
                        .Where(pf => pf.Follower.Uid == cUser.Profile.Uid && pf.Profile.Uid == p.Follower.Uid).Any() : false,
                        Stores = p.Follower.User.Stores.Select(s => new StoreDetailsResponse
                        {
                            Followers = s.StoreFollowers.Count(),
                            Name = s.Name,
                            ImageUrl = s.ImageUrl,
                            Uid = s.Uid,
                            UniqueName = s.UniqueName
                        }).ToList()
                    });

                var list = await PagedList<ProfileDetailsResponse>.ToPagedListAsync(profileFollowers, request.PageNumber, request.PageSize);
                return _mapper.Map<PagingResponse<ProfileDetailsResponse>>(list);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
