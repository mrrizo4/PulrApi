using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Search;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Search.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Search.Queries
{
    public class GetPostsSearchQuery : IRequest<PaginatedResultDto<PostSearchResultDto>>
    {
        public string SearchTerm { get; set; }
        public int Page { get; set; }
        public int? PageSize { get; set; }
    }

    public class GetPostsSearchQueryHandler : IRequestHandler<GetPostsSearchQuery, PaginatedResultDto<PostSearchResultDto>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IMediator _mediator;

        public GetPostsSearchQueryHandler(IApplicationDbContext dbContext, IMediator mediator)
        {
            _dbContext = dbContext;
            _mediator = mediator;
        }

        public async Task<PaginatedResultDto<PostSearchResultDto>> Handle(GetPostsSearchQuery request, CancellationToken cancellationToken)
        {
            try
            {
                //await _mediator.Publish(new CreateSearchHistoryEntryNotification
                //{
                //    Term = request.SearchTerm
                //}, cancellationToken);

                // Create the base query
                var baseQuery = _dbContext.Posts
                    .Where(p => p.IsActive && p.Text.ToLower().Contains(request.SearchTerm.ToLower()));

                // Get total count
                var totalCount = await baseQuery.CountAsync(cancellationToken);

                // Get paginated results
                var items = await baseQuery
                    .OrderBy(p => p.Text)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new PostSearchResultDto
                    {
                        Uid = p.Uid,
                        Caption = p.Text,
                        // Select only specific media properties
                        MediaFile = new MediaFileDto
                        {
                            Url = p.MediaFile.Url,
                            MediaFileType = p.MediaFile.MediaFileType,
                            Uid = p.MediaFile.Uid
                        },
                        LikesCount = p.PostLikes.Count,
                        CreatedAt = p.CreatedAt,
                        Profile = new UserBasicDto
                        {
                            Uid = p.User.Profile.Id,
                            FullName = p.User.FirstName,

                            ImageUrl = p.User.Profile.ImageUrl ?? string.Empty
                        }
                    })
                    .Skip((request.Page - 1) * (request.PageSize ?? totalCount))
                    .Take(request.PageSize ?? totalCount)
                    .ToListAsync(cancellationToken);

                return PaginatedResultDto<PostSearchResultDto>.Create(
                    request.Page,
                    request.PageSize ?? totalCount,
                    totalCount,
                    items);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("An error occurred while searching for posts.", ex);
            }
        }
    }
}
