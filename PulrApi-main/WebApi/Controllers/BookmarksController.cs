using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Mediatr.Bookmarks.Commands.Add;
using Core.Application.Mediatr.Bookmarks.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Authorize(Roles = PulrRoles.User)]
public class BookmarksController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetBookmarkedPost()
    {
        return Ok(await Mediator.Send(new GetBookmarkedPostsForProfileQuery()));
    }

    [HttpPost]
    public async Task<ActionResult> ToggleBookmark([FromBody] ToggleBookmarkCommand command)
    {
        return Ok(await Mediator.Send(command));
    }
}