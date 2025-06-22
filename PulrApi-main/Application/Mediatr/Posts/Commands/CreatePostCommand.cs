using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Posts.Queries;
using Core.Application.Models.Post;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Posts.Commands
{
    public class CreatePostCommand : IRequest<PostDetailsResponse>
    {
        public string StoreUid { get; set; }
        public string Text { get; set; }
        public List<string> Hashtags { get; set; } = new List<string>();
        public List<string> Mentions { get; set; } = new List<string>();
        public double SpotExpiryHours { get; set; } = 0;
        public List<PostProductTagDto> PostProductTags { get; set; } = new List<PostProductTagDto>();
        [Required]
        public string MediaFileUid { get; set; }
    }

    public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, PostDetailsResponse>
    {
        private readonly ILogger<CreatePostCommandHandler> _logger;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;
        private readonly IMediator _mediator;
        public CreatePostCommandHandler(ILogger<CreatePostCommandHandler> logger, IMapper mapper, ICurrentUserService currentUserService, IApplicationDbContext dbContext, IMediator mediator)
        {
            _logger = logger;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
            _mediator = mediator;
        }

        public async Task<PostDetailsResponse> Handle(CreatePostCommand request, CancellationToken cancellationToken)
        {
            CreatePostDto model = _mapper.Map<CreatePostDto>(request);
            try
            {
                var user = await _currentUserService.GetUserAsync(true);

                var taggedProducts = new List<Product>();
                if (model.PostProductTags.Count > 0)
                {
                    var productUids = model.PostProductTags.Select(e => e.ProductUid).ToList();
                    taggedProducts = await _dbContext.Products.Where(p => productUids.Contains(p.Uid) && p.IsActive).ToListAsync(CancellationToken.None);
                };

                var mentionedProfiles = await _dbContext.Profiles.Include(u => u.User).Where(e => model.Mentions.Contains(e.User.UserName)).ToListAsync();
                var mentionedStores = await _dbContext.Stores.Where(e => model.Mentions.Contains(e.UniqueName)).ToListAsync(cancellationToken);

                var existingHashtags = await _dbContext.Hashtags.Where(ht => model.Hashtags.Contains(ht.Value)).ToListAsync(cancellationToken);
                var hashtagsWithoutDuplicates = model.Hashtags
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Where(value => !existingHashtags.Select(eh => eh.Value.Trim().ToLower()).Contains(value.Trim().ToLower()))
                    .ToList();

                var existingMediaFile = await _dbContext.MediaFiles.SingleOrDefaultAsync(mf => mf.Uid == request.MediaFileUid, cancellationToken);
                if(existingMediaFile == null) {
                    throw new NotFoundException("Media file not found");
                }

                var newPost = new Post
                {
                    Text = model.Text,
                    User = user,
                    Store = model.StoreUid != null ? await _dbContext.Stores.SingleOrDefaultAsync(s => s.UserId == user.Id && s.Uid == model.StoreUid, cancellationToken) : null,
                    MediaFile = existingMediaFile,
                    PostProfileMentions = mentionedProfiles.ConvertAll(e => new PostProfileMention { Profile = e }).ToList(),
                    PostStoreMentions = mentionedStores.ConvertAll(e => new PostStoreMention { Store = e }).ToList(),
                    PostProductTags = model.PostProductTags.Select(e => new PostProductTag
                    {
                        PositionLeftPercent = e.PositionLeftPercent,
                        PositionTopPercent = e.PositionTopPercent,
                        Product = taggedProducts.SingleOrDefault(p => p.Uid == e.ProductUid)
                    }).ToList()
                };

                // Create new hashtags
                var newHashtags = hashtagsWithoutDuplicates.Select(val => new Hashtag { Value = val.Trim() }).ToList();
                if (newHashtags.Any())
                {
                    await _dbContext.Hashtags.AddRangeAsync(newHashtags, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                // Create post hashtag relationships
                newPost.PostHashtags = newHashtags.Select(h => new PostHashtag { Hashtag = h }).ToList();
                if (existingHashtags.Any())
                {
                    foreach (var existingHashtag in existingHashtags)
                    {
                        newPost.PostHashtags.Add(new PostHashtag { Hashtag = existingHashtag });
                    }
                }

                _dbContext.Posts.Add(newPost);
                await _dbContext.SaveChangesAsync(CancellationToken.None);

                return await _mediator.Send(new GetPostQuery() { Uid = newPost.Uid });
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
