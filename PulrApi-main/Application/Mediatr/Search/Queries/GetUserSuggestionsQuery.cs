using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Search;
using Core.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

namespace Core.Application.Mediatr.Search.Queries
{
    public class GetUserSuggestionsQuery : IRequest<List<UserSuggestionDto>>
    {
        public string SearchTerm { get; set; }
        public int Limit { get; set; } = 5;
    }

    public class GetUserSuggestionsQueryHandler : IRequestHandler<GetUserSuggestionsQuery, List<UserSuggestionDto>>
    {
        private readonly IApplicationDbContext _dbContext;

        public GetUserSuggestionsQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<UserSuggestionDto>> Handle(GetUserSuggestionsQuery request, CancellationToken cancellationToken)
        {
            var searchTerm = request.SearchTerm?.ToLower() ?? string.Empty;

            var users = await _dbContext.Users
                .Where(u => u.IsVerified && !u.IsSuspended &&
                       (u.UserName.ToLower().Contains(searchTerm) ||
                        u.FirstName.ToLower().Contains(searchTerm)))
                .OrderByDescending(u =>
                    u.UserName.ToLower().StartsWith(searchTerm) ? 2 :
                    (u.FirstName.ToLower().StartsWith(searchTerm) || u.LastName.ToLower().StartsWith(searchTerm)) ? 1 : 0)
                .ThenBy(u => u.UserName.ToLower().IndexOf(searchTerm))
                .ThenBy(u => u.FirstName.ToLower().IndexOf(searchTerm))
                .Take(request.Limit)
                .Select(u => new UserSuggestionDto
                {
                    Uid = u.Id,
                    Username = u.UserName,
                    DisplayName = u.DisplayName,
                    FullName = u.FirstName,
                    ImageUrl = u.Profile.ImageUrl ?? string.Empty,
                    FollowersCount = u.Profile.ProfileFollowers.Count
                })
                .ToListAsync(cancellationToken);

            return users;
        }
    }

    public class UserSuggestionDto
    {
        public string Uid { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string FullName { get; set; }
        public string ImageUrl { get; set; }
        public int FollowersCount { get; set; }
    }
} 