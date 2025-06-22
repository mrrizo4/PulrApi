using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Stories;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Stories.Queries;

public class GetSingleStoryQuery : IRequest<StoryResponse>
{
    public string Uid { get; set; }
}

public class GetSingleStoryQueryHandler : IRequestHandler<GetSingleStoryQuery, StoryResponse>
{
    private readonly ILogger<GetSingleStoryQueryHandler> _logger;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public GetSingleStoryQueryHandler(ILogger<GetSingleStoryQueryHandler> logger, IMapper mapper, ICurrentUserService currentUserService, IApplicationDbContext dbContext)
    {
        _logger = logger;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
    }

    public async Task<StoryResponse> Handle(GetSingleStoryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();
            var now = DateTime.UtcNow;
            var response = await _dbContext.Stories
                .Include(s => s.StorySeens)
                .Include(s => s.User).ThenInclude(u => u.Profile)
                .Where(s => s.Uid == request.Uid)
                .Select(s => new StoryResponse
                {
                    Uid = s.Uid,
                    EntityUid = s.User.Profile.Uid,
                    Text = s.Text,
                    LikedByMe = currentUser != null && currentUser.Profile != null
                                ? s.StoryLikes.Any(l => l.Id == currentUser.Profile.Id)
                                : false,
                    SeenByMe = currentUser != null && currentUser.Profile != null
                                ? s.StorySeens.Any(s => s.SeenById == currentUser.Profile.Id)
                                : false, // Correctly populate SeenByMe
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
                             Name = stp.Product.Name,
                             Price = stp.Product.Price,
                             Uid = stp.Product.Uid,
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
                }).SingleOrDefaultAsync(cancellationToken);

            return response;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting single story");
            throw;
        }
    }
}