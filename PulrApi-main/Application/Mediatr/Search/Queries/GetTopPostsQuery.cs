using Application.DTOs.Search;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Search.Queries;

public class GetTopPostsQuery : IRequest<PaginatedResultDto<PostSearchResultDto>>
{
    public string SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int? PageSize { get; set; }
}

public class GetTopPostsQueryHandler : IRequestHandler<GetTopPostsQuery, PaginatedResultDto<PostSearchResultDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private const int DefaultPageSize = 10;

    public GetTopPostsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedResultDto<PostSearchResultDto>> Handle(GetTopPostsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate and set default values for pagination
            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize ?? DefaultPageSize;
            if (pageSize <= 0) pageSize = DefaultPageSize;

            // Create the base query
            var baseQuery = _dbContext.Posts
                .Where(p => p.IsActive);

            // Add search term filter if provided
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                baseQuery = baseQuery.Where(p => p.Text.ToLower().Contains(request.SearchTerm.ToLower())).OrderBy(p => p.Text);
            }

            // Get total count
            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Get paginated results
            var items = await baseQuery
                .OrderByDescending(p => p.PostLikes.Count)
                .Select(p => new PostSearchResultDto
                {
                    Uid = p.Uid,
                    Caption = p.Text,
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
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return PaginatedResultDto<PostSearchResultDto>.Create(
                page,
                pageSize,
                totalCount,
                items);
        }
        catch (Exception ex)
        {
            // Log the exception (not implemented here)
            throw new Exception("An error occurred while fetching top posts.", ex);
        }
    }
}
