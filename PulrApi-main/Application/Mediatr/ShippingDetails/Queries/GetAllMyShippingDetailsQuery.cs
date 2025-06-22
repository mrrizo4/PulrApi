using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.Extensions.Logging;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mappings;
using Core.Application.Mediatr.ShippingDetails.Queries;
using Core.Application.Models;
using Core.Application.Models.ShippingDetails;

namespace Core.Application.Mediatr.ShippingDetails.Queries
{
    public class GetShippingDetailsQuery : PagingParamsRequest, IRequest<PagingResponse<ShippingDetailsResponse>>
    {
    }

    public class GetShippingDetailsQueryHandler : IRequestHandler<GetShippingDetailsQuery, PagingResponse<ShippingDetailsResponse>>
    {
        private readonly ILogger<GetShippingAddressQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetShippingDetailsQueryHandler(ILogger<GetShippingAddressQueryHandler> logger,
            IApplicationDbContext dbContext, ICurrentUserService currentUserService, IMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<PagingResponse<ShippingDetailsResponse>> Handle(GetShippingDetailsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                if (cUser == null)
                {
                    throw new NotAuthenticatedException("");
                }

                var shippingDetailsList = await _dbContext.ShippingDetails
                    .Where(sd => sd.User == cUser)
                    .ProjectTo<ShippingDetailsResponse>(_mapper.ConfigurationProvider)
                    .PaginatedListAsync(request.PageNumber, request.PageSize);

                return _mapper.Map<PagingResponse<ShippingDetailsResponse>>(shippingDetailsList);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
