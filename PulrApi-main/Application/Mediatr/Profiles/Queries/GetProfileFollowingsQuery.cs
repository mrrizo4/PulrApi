using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using Core.Domain.Views;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Profiles.Queries
{
    public class GetProfileFollowingsQuery : PagingParamsRequest, IRequest<PagingResponse<ProfileDetailsResponse>>
    {
        [Required]
        public string ProfileUid { get; set; }
    }

    public class GetProfileFollowingsQueryHandler : IRequestHandler<GetProfileFollowingsQuery, PagingResponse<ProfileDetailsResponse>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetProfileFollowingsQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetProfileFollowingsQueryHandler(IApplicationDbContext dbContext, 
            ILogger<GetProfileFollowingsQueryHandler> logger, 
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<PagingResponse<ProfileDetailsResponse>> Handle(GetProfileFollowingsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();

                var profile = await _dbContext.Profiles.Where(p => p.Uid == request.ProfileUid).SingleOrDefaultAsync(cancellationToken);
                if (profile == null)
                {
                    throw new BadRequestException("Profile doesnt exist");
                }

                var rawSql = @"WITH
                                ProfileResults AS (
                                    SELECT
                                        p.""Uid"" AS ""Uid"",
                                        false AS ""IsStore"",
                                        pf.""CreatedAt"",
                                        u.""FirstName"",
                                        u.""LastName"",
                                        u.""UserName"" AS ""Username"",
                                        NULL AS ""StoreName"",
                                        NULL AS ""StoreUniqueName"",
                                        u.""Email"" AS ""Email"",
                                        p.""ImageUrl"",
                                        COUNT(pf.""ProfileId"") AS ""Followers"",
                                        COUNT(pf.""FollowerId"") AS ""Following"",
                                        EXISTS (
                                            SELECT
                                                1
                                            FROM
                                                public.""ProfileFollowers"" pf2
                                            WHERE
                                                pf2.""FollowerId"" = {1}
                                                AND pf2.""ProfileId"" = p.""Id""
                                        ) AS ""FollowedByMe"",
                                        NULL AS ""StoreUid"",
                                        NULL AS ""Stores"",
                                        COUNT(
                                            CASE
                                                WHEN posts.""UserId"" = u.""Id"" THEN 1
                                            END
                                        ) AS ""PostsCount"",
                                        p.""About"",
                                        p.""Location"",
                                        0 AS ""ActiveStoriesCount"",
                                        false AS ""IsInfluencer"",
                                        u.""PhoneNumber"" AS ""PhoneNumber""
                                    FROM
                                        public.""ProfileFollowers"" pf
                                        LEFT JOIN public.""Profiles"" p ON pf.""ProfileId"" = p.""Id""
                                        LEFT JOIN public.""AspNetUsers"" u ON p.""UserId"" = u.""Id""
                                        LEFT JOIN public.""Stories"" story ON story.""UserId"" = u.""Id""
                                        LEFT JOIN public.""Posts"" posts ON posts.""UserId"" = u.""Id""
                                    WHERE
                                        pf.""FollowerId"" = {0}
                                    GROUP BY
                                        p.""Uid"",
                                        p.""Id"",
                                        pf.""CreatedAt"",
                                        u.""FirstName"",
                                        u.""LastName"",
                                        u.""Email"",
                                        u.""UserName"",
                                        u.""PhoneNumber"",
                                        p.""ImageUrl"",
                                        p.""About"",
                                        p.""Location""
                                ),
                                StoreResults AS (
                                    SELECT
                                        s.""Uid"" AS ""Uid"",
                                        true AS ""IsStore"",
                                        sf.""CreatedAt"",
                                        NULL AS ""FirstName"",
                                        NULL AS ""LastName"",
                                        NULL as ""Username"",
                                        s.""Name"" AS ""StoreName"",
                                        s.""UniqueName"" AS ""StoreUniqueName"",
                                        s.""StoreEmail"" AS ""Email"",
                                        s.""ImageUrl"",
                                        COUNT(sf.""StoreId"") AS ""Followers"",
                                        0 AS ""Following"",
                                        EXISTS (
                                            SELECT
                                                1
                                            FROM
                                                public.""StoreFollowers"" sf2
                                            WHERE
                                                sf2.""FollowerId"" = COALESCE({1}, -1)
                                                AND sf2.""StoreId"" = s.""Id""
                                        ) AS ""FollowedByMe"",
                                        s.""Uid"" AS ""StoreUid"",
                                        s.""Name"" AS ""Stores"",
                                        COUNT(
                                            CASE
                                                WHEN posts.""StoreId"" = s.""Id"" THEN 1
                                            END
                                        ) AS ""PostsCount"",
                                        NULL AS ""About"",
                                        s.""Location"",
                                        0 AS ""ActiveStoriesCount"",
                                        false AS ""IsInfluencer"",
                                        s.""PhoneNumber"" AS ""PhoneNumber""
                                    FROM
                                        public.""StoreFollowers"" sf
                                        LEFT JOIN public.""Stores"" s ON sf.""StoreId"" = s.""Id""
                                        LEFT JOIN public.""AspNetUsers"" u ON s.""UserId"" = u.""Id""
                                        LEFT JOIN public.""Stories"" story ON story.""StoreId"" = s.""Id""
                                        LEFT JOIN public.""Posts"" posts ON posts.""StoreId"" = s.""Id""
                                    WHERE
                                        sf.""FollowerId"" = {0}
                                    GROUP BY
                                        sf.""CreatedAt"",
                                        s.""Name"",
                                        s.""UniqueName"",
                                        s.""StoreEmail"",
                                        s.""ImageUrl"",
                                        s.""Uid"",
                                        s.""Location"",
                                        s.""Id"",
                                        s.""PhoneNumber""
                                )
                                SELECT
                                    *
                                FROM
                                    ProfileResults
                                UNION
                                SELECT
                                    *
                                FROM
                                    StoreResults
                                ORDER BY
                                    ""CreatedAt"" DESC";

                IQueryable<ProfileFollowingView> profileFollowingsQueryable = _dbContext.ProfileFollowingViews.FromSqlRaw(rawSql, profile.Id, cUser != null ? cUser.Profile.Id : -1);

                var profileAndStoreFollowingsPagedList = await PagedList<ProfileFollowingView>.ToPagedListAsync(profileFollowingsQueryable, request.PageNumber, request.PageSize);

                return _mapper.Map<PagingResponse<ProfileDetailsResponse>>(profileAndStoreFollowingsPagedList);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
