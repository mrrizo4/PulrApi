using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.ShippingDetails.Queries;
using Core.Application.Models.ShippingDetails;

namespace Core.Application.Mediatr.ShippingDetails.Queries;

public class GetDefaultShippingAddressQuery : IRequest<ShippingDetailsResponse>
{
}

public class GetDefaultShippingAddressQueryHandler : IRequestHandler<GetDefaultShippingAddressQuery, ShippingDetailsResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<GetDefaultShippingAddressQueryHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetDefaultShippingAddressQueryHandler(IApplicationDbContext dbContext,
        ILogger<GetDefaultShippingAddressQueryHandler> logger, ICurrentUserService currentUserService, IMapper mapper)
    {
        _dbContext = dbContext;
        _logger = logger;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<ShippingDetailsResponse> Handle(GetDefaultShippingAddressQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cUser = await _currentUserService.GetUserAsync();
            if (cUser == null)
            {
                throw new NotAuthenticatedException("");
            }

            var shippingAddress = await _dbContext.ShippingDetails.SingleOrDefaultAsync(sd => sd.IsActive
                && sd.DefaultShippingAddress && sd.User == cUser, cancellationToken);

            if (shippingAddress == null)
                throw new NotFoundException("Shipping address was not found");


            return _mapper.Map<ShippingDetailsResponse>(shippingAddress);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}
