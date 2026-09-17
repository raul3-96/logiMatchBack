using LogiMatch.Application;
using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Domain;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Trips;

public class CompleteTripHandler
{
    private readonly IApplicationDbContext _dbContext;

    public CompleteTripHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(Guid tripId)
    {
        var trip = await _dbContext.Trips
            .FirstOrDefaultAsync(x => x.Id == tripId);

        if (trip == null)
            throw new NotFoundException(
                "The specified trip does not exist.");

        if (trip.Status != TripStatus.InProgress)
            throw new ConflictException(
                "Only in-progress trips can be completed.");

        var hasActiveCargo = await _dbContext.TripCargos
            .AnyAsync(x =>
                x.TripId == tripId &&
                (x.Status == TripCargoStatus.Reserved ||
                 x.Status == TripCargoStatus.InProgress));

        if (hasActiveCargo)
            throw new ConflictException(
                "The trip cannot be completed because it has active cargo.");

        trip.Complete();

        await _dbContext.SaveChangesAsync();
    }
}