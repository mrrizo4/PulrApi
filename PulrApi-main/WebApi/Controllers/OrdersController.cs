using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Core.Application.Mediatr.Orders.Commands.Create;
using Core.Application.Mediatr.Orders.Queries;
using Core.Application.Models;
using Core.Application.Models.Orders;

namespace WebApi.Controllers;

public class OrdersController : ApiControllerBase
{
    [HttpGet("{storeUid}")]

    public async Task<ActionResult<PagingResponse<OrderResponse>>> GetAllOrdersByStore(string storeUid)
    {
        var res = await Mediator.Send(new GetAllOrdersByStoreQuery() { StoreUid = storeUid });
        return Ok(res);
    }

    // TODO test
    [HttpGet("{uid}")]
    public async Task<ActionResult<OrderDetailsResponse>> GetOrder(string uid)
    {
        var res = await Mediator.Send(new GetOrderQuery() { Uid = uid });
        return Ok(res);
    }

    // TODO test
    [HttpPost]
    public async Task<ActionResult<UidBaseResponse>> CreateOrder([FromBody] CreateOrderCommand command)
    {
        var uid = await Mediator.Send(command);
        return Ok(new UidBaseResponse() { Uid = uid });
    }
}