using Dashboard.Application.Mediatr.Stats.Queries;
using Dashboard.Application.Models.Stats;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebApi.Controllers.Dashboard;

public class StatsController : DashboardApiControllerBase
{

    [HttpGet]
    public async Task<ActionResult<StatsResponse>> GetStats([FromQuery] GetStatsQuery query)
    {
        var res = await Mediator.Send(query);
        return Ok(res);
    }
}