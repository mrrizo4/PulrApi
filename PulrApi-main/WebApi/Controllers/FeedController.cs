using System.Threading.Tasks;
using Core.Application.Mediatr.Feed.Queries;
using Core.Application.Models;
using Core.Application.Models.Post;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

public class FeedController : ApiControllerBase
{
    [HttpGet("following")]
    public async Task<ActionResult<PagingResponse<PostResponse>>> GetUserFollowingFeed([FromQuery] GetUserFollowingFeedQuery query)
    {
        var x = await Mediator.Send(query);
        return Ok(x);
    }

    [HttpGet("for-you")]
    public async Task<ActionResult<PagingResponse<PostResponse>>> GetUserForYouFeed()
    {
        var x = await Mediator.Send(new GetUserForYourFeedQuery());
        return Ok(x);
    }
}