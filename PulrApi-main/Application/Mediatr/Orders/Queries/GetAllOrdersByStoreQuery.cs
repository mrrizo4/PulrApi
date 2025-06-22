using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.BagItems;
using Core.Application.Models.Currencies;
using Core.Application.Models.Orders;
using Core.Application.Models.ShippingDetails;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Orders.Queries
{
    public class GetAllOrdersByStoreQuery : PagingParamsRequest, IRequest<PagingResponse
        <OrderResponse>>
    {
        [Required] public string StoreUid { get; set; }
    }

    public class GetAllOrdersByStoreQueryHandler : IRequestHandler<GetAllOrdersByStoreQuery, PagingResponse<OrderResponse>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetAllOrdersByStoreQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetAllOrdersByStoreQueryHandler(IApplicationDbContext dbContext,
            ILogger<GetAllOrdersByStoreQueryHandler> logger,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<PagingResponse<OrderResponse>> Handle(GetAllOrdersByStoreQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync(false, true);
                if (cUser.Stores.All(s => s.Uid != request.StoreUid))
                {
                    throw new BadRequestException($"Store uid '{request.StoreUid}' not valid.");
                }

                var ordersQuery = _dbContext.Orders
                    .Where(o => o.IsActive && o.OrderProductAffiliates
                        .Select(opa => opa.Product.Store.Uid)
                        .Contains(request.StoreUid))
                    .Select(o => new OrderResponse
                    {
                        Uid = o.Uid,
                        ProfileUid = o.Profile.Uid,
                        ShippingDetails = new ShippingDetailsResponse
                        {
                            Uid = o.ShippingDetails.Uid,
                            Address = o.ShippingDetails.Address,
                            Apartment = o.ShippingDetails.Apartment,
                            City = o.ShippingDetails.City,
                            Country = o.ShippingDetails.Country,
                            Email = o.ShippingDetails.Email,
                            Floor = o.ShippingDetails.Floor,
                            Region = o.ShippingDetails.Region,
                            FullName = o.ShippingDetails.FirstName,
                            FirstName = o.ShippingDetails.FirstName,
                            LastName = o.ShippingDetails.LastName,
                            PhoneNumber = o.ShippingDetails.PhoneNumber,
                            ZipCode = o.ShippingDetails.ZipCode
                        },
                        Currency = new CurrencyDetailsResponse
                        {
                            Code = o.Currency.Code,
                            Name = o.Currency.Name,
                            Uid = o.Currency.Uid,
                        },
                        PaymentMethodUid = o.PaymentMethod.Uid,
                        OrderProductAffiliates = (List<OrderProductAffiliateDto>)o.OrderProductAffiliates.Select(opa =>
                            new OrderProductAffiliateDto
                            {
                                AffiliateId = opa.Affiliate.AffiliateId,
                                Product = new BagProductExtendedDto
                                {
                                    Uid = opa.Product.Uid,
                                    Description = opa.Product.Description,
                                    Name = opa.Product.Name,
                                    Price = opa.Product.Price,
                                    Quantity = opa.Product.Quantity,
                                    BagQuantity = opa.ProductQuantity,
                                }
                            })
                    });

                var orderList = await PagedList<OrderResponse>.ToPagedListAsync(ordersQuery, request.PageNumber, request.PageSize);
                var ordersPagedResponse = _mapper.Map<PagingResponse<OrderResponse>>(orderList);
                ordersPagedResponse.ItemIds = ordersPagedResponse.Items.Select(item => item.Uid).ToList();
                return ordersPagedResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
