using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Search.Notifications;
using Core.Application.Models.Search;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Search.Queries;

public class GetSearchResultsQuery : IRequest<SearchResult>
{
    public string Query { get; set; }
    public int ResultCount { get; set; } = 5;
}

public class GetSearchResultsQueryHandler : IRequestHandler<GetSearchResultsQuery, SearchResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly ILogger<GetSearchResultsQueryHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public GetSearchResultsQueryHandler(IApplicationDbContext dbContext, 
        IMediator mediator, ILogger<GetSearchResultsQueryHandler> logger,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _mediator = mediator;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<SearchResult> Handle(GetSearchResultsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var searchResult = new SearchResult();
            if (String.IsNullOrWhiteSpace(request.Query))
                return searchResult;

            searchResult.Posts = await _dbContext.Posts
                .Where(p => p.IsActive
                            && !p.User.IsSuspended
                            && p.Text.ToLower().Contains(request.Query.ToLower().Trim()))
                .Select(p =>
                    new BaseSearchResult
                    {
                        Uid = p.Uid,
                        Name = p.Text,
                        ImageUrl = p.MediaFile.Url,
                    }).Take(request.ResultCount).ToListAsync(cancellationToken);


            searchResult.Products = await _dbContext.Products
                .Where(p => p.IsActive
                            && !p.Store.User.IsSuspended
                            && (p.Name.ToLower().Contains(request.Query.ToLower().Trim()) ||
                                p.Description.ToLower().Contains(request.Query.ToLower().Trim())))
                .Select(p =>
                    new BaseSearchResult
                    {
                        Uid = p.Uid,
                        Name = p.Name,
                        ImageUrl = p.ProductMediaFiles.OrderBy(mf => mf.MediaFile.Priority)
                            .Select(mf => mf.MediaFile.Url).FirstOrDefault(),
                    }).Take(request.ResultCount).ToListAsync(cancellationToken);


            searchResult.Profiles = await _dbContext.Profiles
                .Where(p => !p.User.IsSuspended
                            && (p.User.FirstName.ToLower().Contains(request.Query.ToLower().Trim()) ||
                                p.User.LastName.ToLower().Contains(request.Query.ToLower().Trim()) ||
                                p.About.ToLower().Contains(request.Query.ToLower().Trim()) ||
                                p.User.CityName.ToLower().Contains(request.Query.ToLower().Trim()) ||
                                p.User.Country.Name.ToLower().Contains(request.Query.ToLower().Trim())
                            ))
                .Select(p =>
                    new ProfileSearchResult
                    {
                        Uid = p.User.Id,
                        Username = p.User.UserName,
                        FullName = p.User.FirstName,
                        Name = p.User.FirstName + " " + p.User.LastName,
                        ImageUrl = p.User.Profile.ImageUrl
                    }).Take(request.ResultCount).ToListAsync(cancellationToken);

            searchResult.Stores = await _dbContext.Stores
                .Where(s => !s.User.IsSuspended
                            && (s.Name.ToLower().Contains(request.Query.ToLower().Trim()) ||
                                s.Description.ToLower().Contains(request.Query.ToLower().Trim()) ||
                                s.LegalName.ToLower().Contains(request.Query.ToLower().Trim()) ||
                                s.UniqueName.ToLower().Contains(request.Query.ToLower().Trim())
                            ))
                .Select(s =>
                    new StoreSearchResult
                    {
                        Uid = s.Uid,
                        Name = s.Name,
                        UniqueName = s.UniqueName,
                        ImageUrl = s.ImageUrl
                    }).Take(request.ResultCount).ToListAsync(cancellationToken);

            if(await _currentUserService.GetUserAsync() != null)
            {
                await _mediator.Publish(new CreateSearchHistoryEntryNotification {Term = request.Query}, cancellationToken);
            }

            return searchResult;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting search results");
            throw;
        }
    }
}