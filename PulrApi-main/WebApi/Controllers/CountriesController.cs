using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Core.Application.Mediatr.Country.Queries;
using Microsoft.AspNetCore.Authorization;
using Core.Application.Models.Country;
using System.Collections.Generic;

namespace WebApi.Controllers
{
    [AllowAnonymous]
    public class CountriesController : ApiControllerBase
    {
        [HttpGet("{uid}")]
        public async Task<ActionResult<CountryDetailsResponse>> Get(string uid)
        {
            var res = await Mediator.Send(new GetCountryQuery() { Uid = uid });
            return Ok(res);
        }

        [HttpGet]
        public async Task<ActionResult<List<CountryResponse>>> GetCountries()
        {
            var res = await Mediator.Send(new GetCountriesQuery());
            return Ok(res);
        }

        // TODO
        [HttpGet("{countryUid}/cities")]
        public async Task<ActionResult<List<CityResponse>>> GetCitiesByCountry(string countryUid)
        {
            var res = await Mediator.Send(new GetCitiesByCountryQuery() { CountryUid = countryUid });
            return Ok(res);
        }
    }
}
