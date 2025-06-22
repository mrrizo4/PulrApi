using System.Threading.Tasks;
using Core.Application.Mediatr.Comments.Commands;
using Core.Application.Mediatr.Comments.Queries;
using Core.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    public class CommentsController : ApiControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<CommentResponse>> CreateComment([FromBody] CreateCommentCommand command)
        {
            var response = await Mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("reply")]
        public async Task<ActionResult<CommentResponse>> ReplyToComment([FromBody] ReplyToCommentCommand command)
        {
            var response = await Mediator.Send(command);
            return Ok(response);
        }

        [HttpPut]
        public async Task<ActionResult<CommentResponse>> UpdateComment([FromBody] UpdateCommentCommand command)
        {
            var response = await Mediator.Send(command);
            return Ok(response);
        }

        [HttpDelete("{uid}")]
        public async Task<ActionResult<DeleteCommentResponse>> DeleteComment(string uid)
        {
            var response = await Mediator.Send(new DeleteCommentCommand { CommentUid = uid });
            return Ok(response);
        }

        [HttpPut("{uid}/toggle-like")]
        public async Task<ActionResult<CommentToggleLikeResponse>> ToggleLike(string uid)
        {
            return Ok(await Mediator.Send(new ToggleCommentLikeCommand { Uid = uid }));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<PagingResponse<CommentResponse>>> GetComments([FromQuery] GetCommentsQuery query)
        {
            var res = await Mediator.Send(query);
            return Ok(res);
        }
    }
}