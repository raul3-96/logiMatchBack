using LogiMatch.Application;
using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Domain;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.TransportRequests;

public class PublishTransportRequestHandler
{
    private readonly IApplicationDbContext _dbContext;

    public PublishTransportRequestHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(Guid requestId)
    {
        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(x => x.Id == requestId);

        if (request == null)
            throw new NotFoundException(
                "The specified transport request does not exist.");

        var hasCargo = await _dbContext.Cargos
            .AnyAsync(x =>
                x.TransportRequestId == requestId);

        if (!hasCargo)
            throw new ConflictException(
                "A transport request must have at least one cargo before it can be published.");

        request.Publish();

        await _dbContext.SaveChangesAsync();
    }
}