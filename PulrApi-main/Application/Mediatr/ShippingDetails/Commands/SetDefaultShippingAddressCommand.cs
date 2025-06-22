using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.ShippingDetails.Commands;

namespace Core.Application.Mediatr.ShippingDetails.Commands;

public class SetDefaultShippingAddressCommand : IRequest <Unit>
{
    public string Uid { get; set; }
}

public class SetDefaultShippingAddressCommandHandler : IRequestHandler<SetDefaultShippingAddressCommand,Unit>
{
    private readonly ILogger<SetDefaultShippingAddressCommandHandler> _logger;
    private readonly IApplicationDbContext _dbContext;

    public SetDefaultShippingAddressCommandHandler(ILogger<SetDefaultShippingAddressCommandHandler> logger,
        IApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Unit> Handle(SetDefaultShippingAddressCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var shippingAddress =
                await _dbContext.ShippingDetails.SingleOrDefaultAsync(sd => sd.IsActive && sd.Uid == request.Uid,
                    cancellationToken);

            if (shippingAddress == null)
                throw new NotFoundException("Shipping address not found");

            shippingAddress.DefaultShippingAddress = true;

            var otherShippingAddresses = await _dbContext.ShippingDetails
                .Where(sd => sd.IsActive && sd != shippingAddress)
                .ToListAsync(cancellationToken);

            foreach (var otherShippingAddress in otherShippingAddresses)
            {
                otherShippingAddress.DefaultShippingAddress = false;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}
