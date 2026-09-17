using LogiMatch.Application;
using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Domain;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Matching;

public class StartTripCargoHandler
{
    private readonly IApplicationDbContext _dbContext;

    public StartTripCargoHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(Guid tripCargoId)
    {
        var tripCargo = await _dbContext.TripCargos
            .FirstOrDefaultAsync(x => x.Id == tripCargoId);

        if (tripCargo == null)
            throw new NotFoundException(
                "The specified trip cargo does not exist.");

        var trip = await _dbContext.Trips
            .FirstOrDefaultAsync(x => x.Id == tripCargo.TripId);

        if (trip == null)
            throw new NotFoundException(
                "The trip associated with the trip cargo does not exist.");

        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(x => x.Id == tripCargo.TransportRequestId);

        if (request == null)
            throw new NotFoundException(
                "The transport request associated with the trip cargo does not exist.");

        if (trip.Status != TripStatus.InProgress)
            throw new ConflictException(
                "The trip must be in progress before starting trip cargo.");

        tripCargo.Start();
        request.Start();

        await _dbContext.SaveChangesAsync();
    }
}