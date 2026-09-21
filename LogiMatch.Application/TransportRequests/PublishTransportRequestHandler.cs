using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.TransportRequests;

public class PublishTransportRequestHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public PublishTransportRequestHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(Guid requestId)
    {
        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(x => x.Id == requestId);

        if (request == null)
            throw new NotFoundException(
                "The specified transport request does not exist.");

        // Validar que el usuario actual es el dueño de la solicitud
        if (request.CustomerId != _currentUserService.UserId)
            throw new ConflictException(
                "You can only publish your own transport requests.");

        var hasCargo = await _dbContext.Cargos
            .AnyAsync(x => x.TransportRequestId == requestId);

        if (!hasCargo)
            throw new ConflictException(
                "A transport request must have at least one cargo before it can be published.");

        request.Publish();

        await _dbContext.SaveChangesAsync();
    }
}