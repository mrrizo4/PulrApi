using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Orders.Queries;
using Core.Application.Models.BagItems;
using Core.Application.Models.Orders;

namespace Core.Application.Mediatr.Orders.Queries
{
    public class GetOrderQuery : IRequest<OrderDetailsResponse>
    {
        [Required] public string Uid { get; set; }
    }

    public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, OrderDetailsResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetOrderQueryHandler> _logger;
        private readonly IMapper _mapper;

        public GetOrderQueryHandler(IApplicationDbContext dbContext,
            ILogger<GetOrderQueryHandler> logger,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<OrderDetailsResponse> Handle(GetOrderQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var orderRes = await _dbContext.Orders.Include(o => o.OrderProductAffiliates)
                    .Include(o => o.OrderProductAffiliates)
                    .SingleOrDefaultAsync(c => c.Uid == request.Uid);
                var orderDto = new OrderDetailsResponse()
                {
                    OrderProductAffiliates = orderRes.OrderProductAffiliates.Select(opa =>
                        new OrderProductAffiliateDto()
                        {
                            AffiliateId = opa.Affiliate.AffiliateId,
                            OrderUid = orderRes.Uid,
                            Product = _mapper.Map<BagProductExtendedDto>(opa.Product),
                        }).ToList()
                };

                foreach (var opa in orderDto.OrderProductAffiliates)
                {
                    opa.Product.AffiliateId = opa.AffiliateId;
                }

                return orderDto;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
