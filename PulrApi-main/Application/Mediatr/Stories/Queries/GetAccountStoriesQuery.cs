using AutoMapper;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Stories;
using Core.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Stories.Queries
{
    public class GetAccountStoriesQuery : IRequest<ProfileWithStoriesResponse>
    {
        [Required]
        public string EntityUid { get; set; }
        public bool IsStore { get; set; }
    }

    public class GetAccountStoriesQueryHandler : IRequestHandler<GetAccountStoriesQuery, ProfileWithStoriesResponse>
    {
        private readonly ILogger<GetAccountStoriesQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;

        public GetAccountStoriesQueryHandler(ILogger<GetAccountStoriesQueryHandler> logger, IApplicationDbContext dbContext, ICurrentUserService currentUserService,
            UserManager<User> userManager,
            IMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<ProfileWithStoriesResponse> Handle(GetAccountStoriesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();

                IQueryable<Story> queryableStories = _dbContext.Stories;

                // TODO optimize query
                // TODO, add logic based on FeedType 

                var dateTimeNow = DateTime.UtcNow;

                if (!request.IsStore)
                {

                    var profileWithStoriesQuery = queryableStories.Where(s => s.User.Profile.Uid == request.EntityUid && !s.User.IsSuspended && s.User.Profile.IsActive && s.User.Stories.Where(s => s.Store == null && s.StoryExpiresIn > dateTimeNow).Any())
                        .OrderByDescending(story => story.User.Stories.Where(p => story.Store == null && story.StoryExpiresIn > dateTimeNow).Take(1).OrderByDescending(e => e.CreatedAt).Select(story => story.CreatedAt).FirstOrDefault());

                    var profileWithStories = await profileWithStoriesQuery.Select(story => new ProfileWithStoriesResponse
                    {
                        Profile = new ProfileForStoryResponse
                        {
                            FullName = story.User.FirstName,
                            FirstName = story.User.FirstName,
                            LastName = story.User.LastName,
                            DisplayName = story.User.DisplayName,
                            ImageUrl = story.User.Profile.ImageUrl,
                            Uid = story.User.Profile.Uid,
                            UserId = story.User.Id,
                            Username = story.User.UserName,
                            LastStoryCreatedAt = story.User.Stories.Where(story => story.StoryExpiresIn > dateTimeNow && story.StoreId == null).Take(1).OrderByDescending(e => e.CreatedAt).Select(p => p.CreatedAt)
                                        .FirstOrDefault(),
                        },
                        Stories = story.User.Stories.Where(story => story.StoryExpiresIn > dateTimeNow && story.StoreId == null)
                        .Take(10)
                        .OrderByDescending(e => e.CreatedAt)
                        .Select(story => new StoryResponse()
                        {
                            Uid = story.Uid,
                            EntityUid = story.User.Profile.Uid,
                            Text = story.Text,
                            LikedByMe = cUser != null && cUser.Profile != null ? story.StoryLikes.Any(l => l.LikedById == cUser.Profile.Id) : false,
                            SeenByMe = cUser != null && cUser.Profile != null ? story.StorySeens.Any(s => s.SeenById == cUser.Profile.Id) : false,
                            LikesCount = story.StoryLikes.Count,
                            //SeenByMe = cUser != null && cUser.Profile != null ? story.StorySeens.Any(s => s.SeenById == cUser.Profile.Id) : false, // New field
                            MediaFile = _mapper.Map<MediaFileDetailsResponse>(story.MediaFile),
                            PostedByStore = false,
                            TaggedProducts = story.StoryProductTags.Select(ppt =>
                                new ProductTagCoordinatesResponse
                                {
                                    PositionLeftPercent = ppt.PositionLeftPercent,
                                    PositionTopPercent = ppt.PositionTopPercent,
                                    Product = new ProductDetailsResponse
                                    {
                                        AffiliateId = ppt.Product.OrderProductAffiliate.Affiliate.AffiliateId,
                                        ArticleCode = ppt.Product.ArticleCode,
                                        Description = ppt.Product.Description,
                                        Name = ppt.Product.Name,
                                        Price = ppt.Product.Price,
                                        Uid = ppt.Product.Uid,
                                        PositionLeftPercent = ppt.PositionLeftPercent,
                                        PositionTopPercent = ppt.PositionTopPercent,
                                        ProductMediaFiles = ppt.Product.ProductMediaFiles.Select(pmf => new MediaFileDetailsResponse()
                                        {
                                            Uid = pmf.MediaFile.Uid,
                                            FileType = pmf.MediaFile.MediaFileType.ToString(),
                                            Url = pmf.MediaFile.Url,
                                            Priority = pmf.MediaFile.Priority
                                        })
                                    }
                                })
                        })
                    }).FirstOrDefaultAsync(cancellationToken);


                    if (profileWithStories != null && cUser != null)
                    {
                        var myFollows = await _dbContext.ProfileFollowers
                            .Where(pf => pf.ProfileId == cUser.Profile.Id)
                            .Select(pf => pf.Profile.Uid).ToListAsync(cancellationToken);

                        profileWithStories.Profile.FollowedByMe = myFollows.Contains(cUser.Profile.Uid);
                        profileWithStories.Profile.IsInfluencer = await _userManager.IsInRoleAsync(new User() { Id = cUser.Id }, PulrRoles.Influencer);
                        profileWithStories.Profile.StoryUids = profileWithStories.Stories.Select(s => s.Uid).ToList();
                    }

                    return profileWithStories;
                }
                else if (request.IsStore)
                {
                    var myStoreStories = await queryableStories
                        .Where(s => !s.User.IsSuspended && s.User.Profile.IsActive && s.Store.Uid == request.EntityUid && s.StoryExpiresIn > dateTimeNow)
                        .OrderByDescending(s =>
                            s.CreatedAt).Select(s => new ProfileWithStoriesResponse()
                            {
                                Profile = new ProfileForStoryResponse()
                                {
                                    StoreName = s.Store.Name,
                                    StoreImageUrl = s.Store.ImageUrl,
                                    StoreUid = s.Store.Uid,
                                    StoreUniqueName = s.Store.UniqueName,
                                    IsStore = true,
                                    LastStoryCreatedAt = s.Store.Stories.Take(1).OrderByDescending(e => e.CreatedAt).Select(story => story.CreatedAt).FirstOrDefault(),
                                },
                                // TODO: check with team limit for stories, for now limited to 10 cause of large query
                                                                // Stories = s.Store.Stories.Where(story => story.StoryExpiresIn > dateTimeNow).Take(10).OrderByDescending(e => e.CreatedAt).Select(story => new StoryResponse()
                                Stories = s.Store.Stories
                                    .Where(story => story.StoryExpiresIn > dateTimeNow)
                                    .OrderByDescending(e => e.CreatedAt)
                                    .Select(story => new StoryResponse()
                                {
                                    Uid = story.Uid,
                                    EntityUid = story.Store.Uid,
                                    Text = story.Text,
                                    LikedByMe = cUser != null && cUser.Profile != null ? story.StoryLikes.Any(l => l.Id == cUser.Profile.Id) : false,
                                    SeenByMe = cUser != null && cUser.Profile != null ? story.StorySeens.Any(s => s.SeenById == cUser.Profile.Id) : false,
                                    LikesCount = story.StoryLikes.Count,
                                    MediaFile = _mapper.Map<MediaFileDetailsResponse>(story.MediaFile),
                                    PostedByStore = true,
                                    TaggedProducts = story.StoryProductTags.Select(ppt =>
                                    new ProductTagCoordinatesResponse
                                    {
                                        PositionLeftPercent = ppt.PositionLeftPercent,
                                        PositionTopPercent = ppt.PositionTopPercent,
                                        Product = new ProductDetailsResponse()
                                        {
                                            AffiliateId = ppt.Product.OrderProductAffiliate.Affiliate.AffiliateId,
                                            ArticleCode = ppt.Product.ArticleCode,
                                            Description = ppt.Product.Description,
                                            Name = ppt.Product.Name,
                                            Price = ppt.Product.Price,
                                            Uid = ppt.Product.Uid,
                                            ProductMediaFiles = ppt.Product.ProductMediaFiles.Select(pmf => new MediaFileDetailsResponse()
                                            {
                                                Uid = pmf.MediaFile.Uid,
                                                FileType = pmf.MediaFile.MediaFileType.ToString(),
                                                Url = pmf.MediaFile.Url,
                                                Priority = pmf.MediaFile.Priority
                                            })
                                        }
                                    }),
                                    CreatedAt = story.CreatedAt
                                })
                            }
                        ).FirstOrDefaultAsync(cancellationToken);

                    if (myStoreStories.Stories.Any())
                    {
                        var myStoreFollows = await _dbContext.StoreFollowers
                            .Where(sf => sf.Store.Uid == request.EntityUid)
                            .Select(sf => sf.Store.Uid).ToListAsync(cancellationToken);

                        if(cUser != null)
                        {
                            myStoreStories.Profile.FollowedByMe = myStoreFollows.Contains(cUser.Profile.Uid);
                        }
                        myStoreStories.Profile.StoryUids = myStoreStories.Stories.Select(s => s.Uid).ToList();
                    };

                    return myStoreStories;
                }

                throw new NotFoundException();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}