﻿using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Dashboard.Application.Models.Posts;
using Microsoft.IdentityModel.Tokens;

namespace Core.Application.Mediatr.Posts.Queries
{
    public class GetHashtagsQuery : IRequest<List<HashtagResponse>>
    {
        public string? SearchTerm { get; set; }
        public int? Limit { get; set; }
    }

    public class GetHashtagsQueryHandler : IRequestHandler<GetHashtagsQuery, List<HashtagResponse>>
    {
        private readonly ILogger<GetHashtagsQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;

        public GetHashtagsQueryHandler(ILogger<GetHashtagsQueryHandler> logger, IApplicationDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<List<HashtagResponse>> Handle(GetHashtagsQuery request, CancellationToken cancellationToken)
        {
            try
            {

                var query = _dbContext.Hashtags
                    .Include(h => h.PostHashtags)
                    .Where(h => h.PostHashtags.Any());

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    query = query.Where(h => h.Value.ToLower().Contains(request.SearchTerm.ToLower()));
                }

                var hashtags = await query
                    .Select(h => new HashtagResponse
                    {
                        Value = h.Value,
                        Count = h.PostHashtags.Count
                    })
                    .OrderBy(h => h.Value)
                    .ThenByDescending(h => h.Count)
                    .Take(request.Limit ?? 50)
                    .ToListAsync(cancellationToken);

                if (hashtags.IsNullOrEmpty() || hashtags.Count == 0)
                {
                    return new List<HashtagResponse>
                    {
                        new HashtagResponse
                        {
                            Value = "There are no posts.",
                            Count = 0
                        }
                    };
                }

                return hashtags;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}