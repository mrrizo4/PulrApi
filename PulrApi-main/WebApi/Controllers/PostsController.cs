using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Mediatr.Posts.Commands;
using Core.Application.Mediatr.Posts.Queries;
using Core.Application.Mediatr.Reports.Commands;
using Core.Application.Models;
using Core.Application.Models.Post;
using Core.Application.Models.Reports;
using Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core.Application.Interfaces;
using System.Collections.Generic;
using Dashboard.Application.Models.Posts;

namespace WebApi.Controllers
{
    public class PostsController : ApiControllerBase
    {
        private readonly IApplicationDbContext _dbContext;

        public PostsController(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [AllowAnonymous]
        [HttpGet("hashtags")]
        public async Task<ActionResult<List<HashtagResponse>>> GetHashtags([FromQuery] string searchTerm, [FromQuery] int? limit)
        {
            var res = await Mediator.Send(new GetHashtagsQuery { SearchTerm = searchTerm, Limit = limit });
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpGet("{uid}")]
        public async Task<ActionResult<PostDetailsResponse>> GetPost(string uid, [FromQuery] string currencyCode)
        {
            var res = await Mediator.Send(new GetPostQuery { Uid = uid, CurrencyCode = currencyCode });
            return Ok(res);
        }

        [HttpPost]
        public async Task<ActionResult<PostDetailsResponse>> CreatePost(CreatePostCommand command)
        {
            var res = await Mediator.Send(command);
            return Ok(res);
        }

        [HttpDelete("{uid}")]
        public async Task<ActionResult> DeletePost(string uid)
        {
            await Mediator.Send(new DeletePostCommand() { Uid = uid });
            return NoContent();
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<PagingResponse<PostResponse>>> GetPosts([FromQuery] GetPostsQuery query)
        {
            var res = await Mediator.Send(query);
            return Ok(res);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPut("{uid}/toggle-like")]
        public async Task<ActionResult<ToggleLikePostResponse>> ToggleLikePost(string uid)
        {
            var res = await Mediator.Send(new ToggleLikePostCommand() { PostUid = uid });
            return Ok(res);
        }

        [HttpPost("share")]
        public async Task<ActionResult<PostResponse>> SharePost(SharePostCommand command)
        {
            var postResponse = await Mediator.Send(command);
            return Ok(postResponse);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPost("{uid}/report")]
        public async Task<ActionResult<ReportResponse>> ReportPost(string uid)
        {
            // First check if it's a story
            var story = await _dbContext.Stories.FirstOrDefaultAsync(s => s.Uid == uid);
            if (story != null)
            {
                var responseS = await Mediator.Send(new ReportEntityCommand { EntityUid = uid, Type = ReportTypeEnum.Story });
                return Ok(responseS);
            }

            // If not a story, treat it as a post
            var response = await Mediator.Send(new ReportEntityCommand { EntityUid = uid, Type = ReportTypeEnum.Post });
            return Ok(response);
        }
    }
}