using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.ShippingDetails.Commands;

namespace Core.Application.Mediatr.ShippingDetails.Commands
{
    public class UpdateMyShippingDetailsCommand : IRequest <Unit>
    {
        [Required] public string Uid { get; set; }
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
        [Required]  public bool DefaultShippingAddress { get; set; }
    }

    public class UpdateMyShippingDetailsCommandHandler : IRequestHandler<UpdateMyShippingDetailsCommand, Unit>
    {
        private readonly ILogger<UpdateMyShippingDetailsCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public UpdateMyShippingDetailsCommandHandler(ILogger<UpdateMyShippingDetailsCommandHandler> logger,
            IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(UpdateMyShippingDetailsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                if (cUser == null)
                {
                    throw new NotAuthenticatedException("");
                }

                var existingShippingDetails = await _dbContext.ShippingDetails.SingleOrDefaultAsync(sd => sd.UserId == cUser.Id && sd.Uid == request.Uid, cancellationToken);
                
                if (existingShippingDetails != null)
                {
                    if (request.DefaultShippingAddress)
                    {
                        var existingShippingAddresses = await _dbContext.ShippingDetails
                            .Where(sd => sd.IsActive && sd.Uid != request.Uid && sd.DefaultShippingAddress && sd.User == cUser)
                            .ToListAsync(cancellationToken);
                        
                        foreach (var existingShippingAddress in existingShippingAddresses)
                        {
                            existingShippingAddress.DefaultShippingAddress = false;
                        }
                    }

                    existingShippingDetails.FirstName = request.FirstName;
                    existingShippingDetails.LastName = request.LastName;
                    existingShippingDetails.Email = request.Email;
                    existingShippingDetails.PhoneNumber = request.PhoneNumber;
                    existingShippingDetails.Address = request.Address;
                    existingShippingDetails.Floor = request.Floor;
                    existingShippingDetails.Apartment = request.Apartment;
                    existingShippingDetails.City = request.City;
                    existingShippingDetails.Country = request.Country;
                    existingShippingDetails.Region = request.Region;
                    existingShippingDetails.ZipCode = request.ZipCode;
                    existingShippingDetails.DefaultShippingAddress = request.DefaultShippingAddress;
                    
                    await _dbContext.SaveChangesAsync(cancellationToken);                 
                }

                return Unit.Value;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
