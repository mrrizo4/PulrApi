using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Core.Application.Mediatr.ShippingDetails.Commands;
using Core.Application.Mediatr.ShippingDetails.Queries;
using Core.Application.Models.ShippingDetails;
using Core.Application.Models;

namespace WebApi.Controllers;

[Route("api/user-shipping-details")]
public class UserShippingDetailsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagingResponse<ShippingDetailsResponse>>> GetShippingDetails()
    {
        var res = await Mediator.Send(new GetShippingDetailsQuery());
        return Ok(res);
    }

    [HttpGet("{uid}")]
    public async Task<ActionResult<ShippingDetailsResponse>> GetShippingAddress(string uid)
    {
        var res = await Mediator.Send(new GetShippingAddressQuery { Uid = uid });
        return Ok(res);
    }

    [HttpGet("default")]
    public async Task<ActionResult<ShippingDetailsResponse>> GetDefaultShippingAddress()
    {
        var res = await Mediator.Send(new GetDefaultShippingAddressQuery());
        return Ok(res);
    }

    [HttpPost]
    public async Task<ActionResult<UidBaseResponse>> CreateShippingAddress([FromBody] CreateShippingAddressCommand command)
    {
        var uid = await Mediator.Send(command);
        return Ok(new UidBaseResponse () { Uid = uid });
    }

    [HttpPut]
    public async Task<ActionResult<NoContentResult>> UpdateShippingDetails([FromBody] UpdateMyShippingDetailsCommand request)
    {
        await Mediator.Send(request);
        return NoContent();
    }

    [HttpPatch("{uid}")]
    public async Task<ActionResult<NoContentResult>> UpdateShippingDetails(string uid)
    {
        await Mediator.Send(new SetDefaultShippingAddressCommand {Uid = uid});
        return NoContent();
    }

    [HttpDelete("{uid}")]
    public async Task<ActionResult<NoContentResult>> DeleteShippingDetails(string uid)
    {
        await Mediator.Send(new DeleteMyShippingDetailsCommand { Uid = uid });
        return NoContent();
    }
}