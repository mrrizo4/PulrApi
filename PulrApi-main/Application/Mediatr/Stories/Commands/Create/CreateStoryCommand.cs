using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Stories.Queries;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Stories;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Stories.Commands.Create;

public class CreateStoryCommand : IRequest<StoryWithProfileResponse>
{
    public string Text { get; set; }
    public string StoreUid { get; set; }
    public List<StoryProductTagDto> StoryProductTags { get; set; } = new List<StoryProductTagDto>();
    public List<string> ProfileMentions { get; set; } = new List<string>();
    public List<string> HashTags { get; set; } = new List<string>();
    public string MediaFileUid { get; set; }
}

public class CreateStoryCommandHandler : IRequestHandler<CreateStoryCommand, StoryWithProfileResponse>
{
    private readonly ILogger<CreateStoryCommandHandler> _logger;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public CreateStoryCommandHandler(ILogger<CreateStoryCommandHandler> logger,
        IMapper mapper, ICurrentUserService currentUserService, IApplicationDbContext dbContext)
    {
        _logger = logger;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
    }

    public async Task<StoryWithProfileResponse> Handle(CreateStoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();

            var mediaFile = await _dbContext.MediaFiles.SingleOrDefaultAsync(
                mf => mf.IsActive && mf.Uid == request.MediaFileUid,
                cancellationToken);

            var store = await _dbContext.Stores.SingleOrDefaultAsync(s => s.UserId == currentUser.Id &&
                                                                          s.IsActive && 
                                                                          s.Uid == request.StoreUid, cancellationToken);
            if(store == null && !String.IsNullOrEmpty(request.StoreUid))
            {
                throw new BadRequestException($"Store with uid {request.StoreUid} not found");
            }

            var mentionedProfiles = await GetMentionedProfiles(request.ProfileMentions, cancellationToken);
            var taggedProducts = await GetTaggedProducts(request.StoryProductTags, cancellationToken);
            var hashTags = await GetStoryHashtags(request.HashTags, cancellationToken);

            var story = new Story
            {
                Text = request.Text,
                StoryExpiresIn = DateTime.UtcNow.AddHours(24),
                MediaFile = mediaFile,
                StoryProductTags = taggedProducts,
                StoryProfileMentions = mentionedProfiles,
                User = currentUser,
                Store = store,
                StoryHashTags = hashTags,
            };

            _dbContext.Stories.Add(story);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var response = await _dbContext.Stories.Where(s => s.Uid == story.Uid)
                .Select(s =>
                new StoryWithProfileResponse() {
                Profile = new ProfileForStoryResponse()
                {
                    FullName = s.User.FirstName,
                    FirstName = s.User.FirstName,
                    LastName = s.User.LastName,
                    DisplayName = s.User.DisplayName,
                    ImageUrl = s.User.Profile.ImageUrl,
                    Uid = s.User.Profile.Uid,
                    UserId = s.User.Id,
                    Username = s.User.UserName,
                    LastStoryCreatedAt = story.CreatedAt,
                },
                Story = new StoryResponse
                {
                    Uid = s.Uid,
                    EntityUid = s.Uid,
                    Text = s.Text,
                    DisplayName = s.User.DisplayName,
                    LikedByMe = currentUser != null && currentUser.Profile != null && s.StoryLikes.Any(l => l.Id == currentUser.Profile.Id),
                    LikesCount = s.StoryLikes.Count,
                    MediaFile = _mapper.Map<MediaFileDetailsResponse>(s.MediaFile),
                    PostedByStore = true,
                    TaggedProducts = s.StoryProductTags.Select(stp =>
                    new ProductTagCoordinatesResponse
                    {
                        PositionLeftPercent = stp.PositionLeftPercent,
                        PositionTopPercent = stp.PositionTopPercent,
                        Product = new ProductDetailsResponse
                        {
                            AffiliateId = stp.Product.OrderProductAffiliate.Affiliate.AffiliateId,
                            ArticleCode = stp.Product.ArticleCode,
                            Description = stp.Product.Description,
                            Name = stp.Product.Name,
                            Price = stp.Product.Price,
                            Uid = stp.Product.Uid,
                            PositionLeftPercent = stp.PositionLeftPercent,
                            PositionTopPercent = stp.PositionTopPercent,
                            ProductMediaFiles = stp.Product.ProductMediaFiles.Select(pmf =>
                                new MediaFileDetailsResponse
                                {
                                    Uid = pmf.MediaFile.Uid,
                                    FileType = pmf.MediaFile.MediaFileType.ToString(),
                                    Url = pmf.MediaFile.Url,
                                    Priority = pmf.MediaFile.Priority
                                })
                        }
                    }),
                    CreatedAt = s.CreatedAt
                }
                }
                ).SingleOrDefaultAsync(cancellationToken);

            return response;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating new story");
            throw;
        }
    }

    private async Task<List<StoryProductTag>> GetTaggedProducts(List<StoryProductTagDto> spotProductTags, CancellationToken cancellationToken)
    {
        var spotTags = new List<StoryProductTag>();
        foreach (var spotProductTag in spotProductTags)
        {
            var product = await _dbContext.Products.SingleOrDefaultAsync(p =>
                    p.IsActive
                    && p.Uid == spotProductTag.ProductUid
                , cancellationToken);

            if (product == null)
                continue;

            spotTags.Add(new StoryProductTag
            {
                Product = product,
                PositionTopPercent = spotProductTag.PositionTopPercent,
                PositionLeftPercent = spotProductTag.PositionLeftPercent,
                AffiliateId = spotProductTag.AffiliateId
            });
        }

        return spotTags;
    }

    private async Task<List<StoryProfileMention>> GetMentionedProfiles(List<string> profileMentions,
        CancellationToken cancellationToken)
    {
        var mentionedProfiles = new List<StoryProfileMention>();
        foreach (var profileUid in profileMentions)
        {
            var profile = await _dbContext.Profiles.SingleOrDefaultAsync(p =>
                    p.IsActive
                    && p.Uid == profileUid
                , cancellationToken);

            if (profile == null)
                continue;

            mentionedProfiles.Add(new StoryProfileMention
            {
                Profile = profile
            });
        }

        return mentionedProfiles;
    }

    private async Task<List<StoryHashTag>> GetStoryHashtags(List<string> spotHashTags,
        CancellationToken cancellationToken)
    {
        var spotTags = new List<StoryHashTag>();
        foreach (var hashTag in spotHashTags)
        {
            var tag = await _dbContext.Hashtags.SingleOrDefaultAsync(h =>
                    h.Value.Trim().ToLower() == hashTag.Trim().ToLower()
                , cancellationToken);

            if (tag == null)
            {
                tag = new Hashtag
                {
                    Value = hashTag.Trim().ToLower()
                };

                _dbContext.Hashtags.Add(tag);
            }

            spotTags.Add(new StoryHashTag
            {
                Hashtag = tag
            });
        }

        return spotTags;
    }
}