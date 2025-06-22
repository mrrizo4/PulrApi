using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Core.Application.Mediatr.BagItems.Commands;
using Core.Application.Mediatr.BagItems.Queries;
using Core.Application.Models.BagItems;

namespace WebApi.Controllers
{
    [Route("api/bag-items")]
    [ApiController]
    public class BagItemsController : ApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<BagResponse>> GetBagItems()
        {
            var res = await Mediator.Send(new GetBagItemsQuery());
            return Ok(res);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateBagItems([FromBody] UpdateBagItemsCommand request)
        {
            await Mediator.Send(request);
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("calculate-exchange-rates")]
        public async Task<ActionResult<BagItemsExchangeRatesResponse>> CalculateExchangeRates([FromBody] CalculateExchangeRatesBagItemsCommand request)
        {
            var res = await Mediator.Send(request);
            return Ok(res);
        }
    }
}
