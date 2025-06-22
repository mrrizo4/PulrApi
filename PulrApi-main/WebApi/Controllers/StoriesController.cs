using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Application.Mediatr.Stories.Commands.Create;
using Core.Application.Mediatr.Stories.Commands.Delete;
using Core.Application.Mediatr.Stories.Commands.MarkStoryAsSeen;
using Core.Application.Mediatr.Stories.Commands.SharePostAsStory;
using Core.Application.Mediatr.Stories.Commands.ToggleLike;
using Core.Application.Mediatr.Stories.Queries;
using Core.Application.Models.Stories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

public class StoriesController : ApiControllerBase
{
    
    [AllowAnonymous]
    [HttpGet("feed")]
    public async Task<ActionResult<List<ProfileWithStoriesResponse>>> GetFeedStories([FromQuery] int limit)
    {
        var res = await Mediator.Send(new GetFeedStoriesQuery { Limit = limit });
        return Ok(res);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ProfileWithStoriesResponse>> GetAccountStories([FromQuery] bool isStore, [FromQuery]string entityUid)
    {
        var res = await Mediator.Send(new GetAccountStoriesQuery() { IsStore = isStore, EntityUid = entityUid  });
        return Ok(res);
    }

    [HttpGet("for-you")]
    public Task<IActionResult> GetForYouStories()
    {
        throw new NotImplementedException();
    }

    [HttpGet("{uid}")]
    public async Task<ActionResult<StoryResponse>> GetSingleStory(string uid)
    {
        return Ok(await Mediator.Send(new GetSingleStoryQuery { Uid = uid }));
    }

    [HttpPost("share-post-as-story")]
    public async Task<ActionResult<StoryResponse>> ShareYourPostAsStory([FromBody] SharePostAsStoryCommand command)
    {
        return Ok(await Mediator.Send(command));
    }
    
    [HttpPost]
    public async Task<ActionResult<StoryResponse>> CreateStory([FromBody] CreateStoryCommand command)
    {
        return Ok(await Mediator.Send(command));
    }

    [HttpDelete("{uid}")]
    public async Task<IActionResult> DeleteStory(string uid)
    {
        await Mediator.Send(new DeleteStoryCommand { Uid = uid });
        return NoContent();
    }

    [HttpPut("{uid}/toggle-like")]
    public async Task<ActionResult<StoryToggleLikeResponse>> ToggleLikeStory(string uid)
    {
        var res = await Mediator.Send(new StoryToggleLikeCommand { StoryUid = uid });
        return Ok(res);
    }

    [HttpPost("mark-as-seen")]
    public async Task<IActionResult> MarkStoryAsSeen([FromBody] MarkStoryAsSeenCommand command)
    {
        await Mediator.Send(command);
        return Ok(new
        {
            StoryUid = command.StoryUid,
            Message = "Story marked as seen successfully.",
            Success = true
        });
    }
}