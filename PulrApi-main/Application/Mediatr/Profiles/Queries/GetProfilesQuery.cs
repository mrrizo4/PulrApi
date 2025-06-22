using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Profiles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Profile = Core.Domain.Entities.Profile;

namespace Core.Application.Mediatr.Profiles.Queries
{
    public class GetProfilesQuery : PagingParamsRequest, IRequest<PagingResponse<ProfileDetailsResponse>>
    {
    }

    public class GetProfilesQueryHandler : IRequestHandler<GetProfilesQuery, PagingResponse<ProfileDetailsResponse>>
    {
        private readonly ILogger<GetProfilesQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IQueryHelperService _queryHelperService;
        private readonly IMapper _mapper;

        public GetProfilesQueryHandler(ILogger<GetProfilesQueryHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IQueryHelperService queryHelperService,
            IMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _queryHelperService = queryHelperService;
            _mapper = mapper;
        }

        public async Task<PagingResponse<ProfileDetailsResponse>> Handle(GetProfilesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<Profile> query = _dbContext.Profiles.Where(p => !p.User.IsSuspended).AsNoTracking();

                // TODO  test predicate
                Expression<Func<Profile, bool>> predicate = null;
                if (predicate != null)
                {
                    query = query.Where(predicate);
                }

                var cUser = await _currentUserService.GetUserAsync();

                Expression<Func<Profile, bool>> predicateDontQuerySelf = e => e.IsActive == true;
                if (cUser?.Profile != null)
                {
                    predicateDontQuerySelf = e => e.IsActive == true && e.Uid != cUser.Profile.Uid;
                }

                query = query.Where(predicateDontQuerySelf);

                if (!String.IsNullOrWhiteSpace(request.Search))
                {
                    query = query.Where(p => !p.User.IsSuspended
                                             && (p.User.FirstName.ToLower().Contains(request.Search.ToLower().Trim()) ||
                                                 p.User.LastName.ToLower().Contains(request.Search.ToLower().Trim()) ||
                                                 p.About.ToLower().Contains(request.Search.ToLower().Trim()) ||
                                                 p.User.CityName.ToLower().Contains(request.Search.ToLower().Trim()) ||
                                                 p.User.Country.Name.ToLower().Contains(request.Search.ToLower().Trim())
                                             ));
                }

                if (string.IsNullOrWhiteSpace(request.Order) || string.IsNullOrWhiteSpace(request.OrderBy))
                {
                    query = query.OrderByDescending(u => u.Id);
                }
                else
                {
                    query = _queryHelperService.AppendOrderBy(query, request.OrderBy, request.Order);
                }

                var queryMapped = query.Select(c => new ProfileDetailsResponse
                {
                    Uid = c.Uid,
                    Followers = c.ProfileFollowers.Count(),
                    Following = c.ProfileFollowings.Count(),
                    FullName = c.User.FirstName,
                    FirstName = c.User.FirstName,
                    LastName = c.User.LastName,
                    Username = c.User.UserName,
                    ImageUrl = c.ImageUrl
                });

                var list = await PagedList<ProfileDetailsResponse>.ToPagedListAsync(queryMapped, request.PageNumber,
                    request.PageSize);

                var profilesPagedResponse = _mapper.Map<PagingResponse<ProfileDetailsResponse>>(list);
                profilesPagedResponse.ItemIds = profilesPagedResponse.Items.Select(item => item.Uid).ToList();
                return profilesPagedResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}