using System;
using System.ComponentModel.DataAnnotations;
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

public class CreateShippingAddressCommand : IRequest<string>
{
    [Required] public string FirstName { get; set; }
    [Required] public string LastName { get; set; }
    [Required] public string Email { get; set; }
    [Required] public string Address { get; set; }
    public string Apartment { get; set; }
    public string Floor { get; set; }
    [Required] public string City { get; set; }
    public string Region { get; set; }
    [Required] public string Country { get; set; }
    [Required] public string ZipCode { get; set; }
    [Required] public string PhoneNumber { get; set; }
    [Required] public bool DefaultShippingAddress { get; set; }
}

public class CreateShippingAddressCommandHandler : IRequestHandler<CreateShippingAddressCommand, string>
{
    private readonly ILogger<CreateShippingAddressCommandHandler> _logger;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateShippingAddressCommandHandler(
        ILogger<CreateShippingAddressCommandHandler> logger,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<string> Handle(CreateShippingAddressCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cUser = await _currentUserService.GetUserAsync();
            if (cUser == null)
            {
                throw new NotAuthenticatedException("");
            }

            if (request.DefaultShippingAddress)
            {
                var existingShippingAddresses = await _dbContext.ShippingDetails
                    .Where(sd => sd.IsActive && sd.DefaultShippingAddress && sd.User == cUser)
                    .ToListAsync(cancellationToken);

                foreach (var existingShippingAddress in existingShippingAddresses)
                {
                    existingShippingAddress.DefaultShippingAddress = false;
                }
            }

            var shippingDetail = new Domain.Entities.ShippingDetails
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                Floor = request.Floor,
                Apartment = request.Apartment,
                City = request.City,
                Country = request.Country,
                Region = request.Region,
                ZipCode = request.ZipCode,
                DefaultShippingAddress = request.DefaultShippingAddress,
                User = cUser
            };
            _dbContext.ShippingDetails.Add(shippingDetail);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return shippingDetail.Uid;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}
