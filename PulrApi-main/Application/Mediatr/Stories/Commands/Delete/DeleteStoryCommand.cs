using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Stories.Commands.Delete;

public class DeleteStoryCommand : IRequest <Unit>
{
    public string Uid { get; set; }
}

public class DeleteStoryCommandHandler : IRequestHandler<DeleteStoryCommand,Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<DeleteStoryCommandHandler> _logger;

    public DeleteStoryCommandHandler(IApplicationDbContext dbContext, ILogger<DeleteStoryCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteStoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var story = await _dbContext.Stories.SingleOrDefaultAsync(s => s.IsActive && s.Uid == request.Uid, cancellationToken);

            if (story == null)
                throw new NotFoundException("Story not found");

            _dbContext.Stories.Remove(story);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deleting the story");
            throw;
        }
    }
}