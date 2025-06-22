using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Core.Application.Mediatr.Currencies.Queries;
using Core.Application.Models.Currencies;

namespace WebApi.Controllers
{
    public class CurrenciesController : ApiControllerBase
    {
        [AllowAnonymous]
        [HttpGet("{uid}")]
        public async Task<ActionResult<CurrencyDetailsResponse>> GetCurrency(string uid)
        {
            var res = await Mediator.Send(new GetCurrencyQuery() { Uid = uid });
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<AllCurrenciesResponse>> GetCurrencies()
        {
            var res = await Mediator.Send(new GetCurrenciesQuery());
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpGet("global")]
        public async Task<ActionResult<CurrencyDetailsResponse>> GetGlobalCurrency()
        {
            var res = await Mediator.Send(new GetGlobalCurrencyQuery());
            return Ok(res);
        }

    }
}
