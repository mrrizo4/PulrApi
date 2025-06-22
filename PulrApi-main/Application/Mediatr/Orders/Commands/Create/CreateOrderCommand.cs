using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Orders.Commands.Create;
using Core.Application.Models.Orders;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Orders.Commands.Create;


public class CreateOrderCommand : IRequest<string>, ICloneable
{
    public PaymentMethodEnum PaymentMethod { get; set; }
    public string CurrencyUid { get; set; }
    public CardDetailsDto CardDetails { get; set; }
    public List<OrderProductDto> Products { get; set; } = new List<OrderProductDto>();

    public object Clone()
    {
        var codSafeClone = (CreateOrderCommand)MemberwiseClone();
        // we skip card details when logging
        codSafeClone.CardDetails = null;
        return codSafeClone;
    }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, string>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public CreateOrderCommandHandler(IApplicationDbContext dbContext, ILogger<CreateOrderCommandHandler> logger, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<string> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cUser = await _currentUserService.GetUserAsync(true);

            var paymentMethod = await _dbContext.PaymentMethods.SingleOrDefaultAsync(e => e.Key == request.PaymentMethod.ToString());

            var profile = await _dbContext.Profiles.Where(p => p.User.Id == cUser.Id).Include(p => p.User).ThenInclude(u => u.ShippingDetails).SingleOrDefaultAsync();
            if (profile == null)
            {
                throw new NotFoundException("Profile not found.");
            }

            if (profile.User.ShippingDetails == null || profile.User.ShippingDetails.Count == 0)
            {
                throw new NotFoundException($"User doesn't have shipping details.");
            }

            var currency = await _dbContext.Currencies.SingleOrDefaultAsync(e => e.Uid == request.CurrencyUid, cancellationToken);

            var orderProductAffiliates = new List<OrderProductAffiliate>();

            foreach (var productDto in request.Products)
            {
                var existingProduct = await _dbContext.Products.SingleOrDefaultAsync(p => p.Uid == productDto.Uid, cancellationToken);
                if (existingProduct == null)
                {
                    throw new NotFoundException($"Product with uid {productDto.Uid} doesn't exist.");
                }

                var opAff = new OrderProductAffiliate()
                {
                    Affiliate = await _dbContext.Affiliates.SingleOrDefaultAsync(a => a.AffiliateId == productDto.AffiliateId, cancellationToken),
                    Product = existingProduct,
                    ProductQuantity = productDto.BagQuantity
                };


                if (opAff != null)
                {
                    orderProductAffiliates.Add(opAff);
                }
            }

            // create order at pulr
            var order = new Order()
            {
                OrderProductAffiliates = orderProductAffiliates,
                Currency = currency,
                PaymentMethod = paymentMethod,
                Profile = profile,
                ShippingDetails = profile.User.ShippingDetails.Single(e => e.DefaultShippingAddress == true),
                RawRequest = JsonConvert.SerializeObject(request.Clone()),
                OrderStatus = OrderStatusEnum.Pending
            };

            _dbContext.Orders.Add(order);

            await _dbContext.SaveChangesAsync(cancellationToken);

            // TODO create order externally (call Stripe)
            // then update order status based on external api response

            return order.Uid;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}
