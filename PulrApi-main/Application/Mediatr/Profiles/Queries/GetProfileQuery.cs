using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Application.Models.Stories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Profiles.Queries
{
    public class GetProfileQuery : IRequest<ProfileDetailsResponse>
    {
        [Required]
        public string Username { get; set; }
    }

    public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, ProfileDetailsResponse>
    {
        private readonly ILogger<GetProfileQueryHandler> _logger;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public GetProfileQueryHandler(ILogger<GetProfileQueryHandler> logger,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<ProfileDetailsResponse> Handle(GetProfileQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                var dateTimeNow = DateTime.Now;
                var profileDto = await _dbContext.Profiles
                    .Where(p => p.User.UserName == request.Username && p.IsActive == true)
                    .Select(p => new ProfileDetailsResponse
                    {
                        Uid = p.Uid,
                        FullName = p.User.FirstName,
                        FirstName = p.User.FirstName,
                        LastName = p.User.LastName,
                        DisplayName = p.User.DisplayName,
                        Username = p.User.UserName,
                        Email = p.User.Email,
                        ImageUrl = p.ImageUrl,
                        Followers = p.ProfileFollowers.Count(),
                        Following = p.ProfileFollowings.Count(),
                        PostsCount = p.User.Posts.Count(),
                        About = p.About,
                        Location = p.Location,
                        WebsiteUrl = p.ProfileSocialMedia.WebsiteUrl,
                        InstagramUrl = p.ProfileSocialMedia.InstagramUrl,
                        FacebookUrl = p.ProfileSocialMedia.FacebookUrl,
                        TwitterUrl = p.ProfileSocialMedia.TwitterUrl,
                        TikTokUrl = p.ProfileSocialMedia.TikTokUrl,
                        SocialMediaLinks = p.ProfileSocialMediaLinks
                            .Select(l => new ProfileSocialMediaLinkDto
                            {
                                Url = l.Url,
                                Title = l.Title,
                                Type = l.Type,
                            }).ToList(),
                        ActiveStoriesCount = p.User.Stories.Count,
                        Stores = p.User.Stores.Select(s => new StoreDetailsResponse
                        {
                            Followers = s.StoreFollowers.Count(),
                            Name = s.Name,
                            ImageUrl = s.ImageUrl,
                            Uid = s.Uid,
                            UniqueName = s.UniqueName
                        }).ToList()
                    }).SingleOrDefaultAsync(cancellationToken);

                if (cUser != null)
                {
                    profileDto.FollowedByMe = await _dbContext.ProfileFollowers
                        .Where(pf => pf.Follower.Uid == cUser.Profile.Uid && pf.Profile.Uid == profileDto.Uid).AnyAsync(cancellationToken);

                    profileDto.FollowedBy = await _dbContext.ProfileFollowers.Where(pf => pf.FollowerId == cUser.Profile.Id || pf.Follower.Uid == profileDto.Uid)
                        .OrderByDescending(pf => pf.Profile.ProfileFollowers.Count).Select(pf => pf.Profile.User.UserName).ToListAsync(cancellationToken);
                }

                return profileDto;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}