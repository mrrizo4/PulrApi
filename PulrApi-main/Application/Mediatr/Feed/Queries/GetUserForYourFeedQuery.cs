using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Post;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Feed.Queries;

public class GetUserForYourFeedQuery : PagingParamsRequest, IRequest<PagingResponse<PostResponse>>
{
}

public class GetUserForYourFeedQueryHandler : IRequestHandler<GetUserForYourFeedQuery, PagingResponse<PostResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<GetUserForYourFeedQueryHandler> _logger;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public GetUserForYourFeedQueryHandler(IApplicationDbContext dbContext,
        ILogger<GetUserForYourFeedQueryHandler> logger, IMapper mapper, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    public async Task<PagingResponse<PostResponse>> Handle(GetUserForYourFeedQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();
            currentUser.Profile = await _dbContext.Profiles
                .SingleOrDefaultAsync(p => p.IsActive && p.UserId == currentUser.Id, cancellationToken);

            
            var postsPagedResponse = new PagingResponse<PostResponse>();
            postsPagedResponse.ItemIds = postsPagedResponse.Items.Select(item => item.Uid).ToList();
            return postsPagedResponse;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting user for you feed with error message {message}", e.Message);
            throw;
        }
    }
}