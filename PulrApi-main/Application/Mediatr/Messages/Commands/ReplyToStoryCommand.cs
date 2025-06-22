using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Messages.Commands
{
    public class ReplyToStoryCommand : IRequest<Unit>
    {
        [Required]
        public string StoryUid { get; set; }
        public string Message { get; set; }
    }

    public class ReplyToStoryCommandHandler : IRequestHandler<ReplyToStoryCommand, Unit>
    {
        public async Task<Unit> Handle(ReplyToStoryCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // TODO
                await Task.Delay(1000, cancellationToken);
                return Unit.Value;
            }
            catch (Exception e)
            {

                throw new Exception($"Error replying to story: {e.Message}", e);
            }
        }
    }
}
