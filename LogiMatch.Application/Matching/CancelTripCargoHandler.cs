using LogiMatch.Application;
using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Domain;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Matching;

public class CancelTripCargoHandler
{
    private readonly IApplicationDbContext _dbContext;

    public CancelTripCargoHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(Guid tripCargoId)
    {
        // 1. Buscar el TripCargo
        var tripCargo = await _dbContext.TripCargos
            .FirstOrDefaultAsync(x => x.Id == tripCargoId);

        if (tripCargo == null)
            throw new NotFoundException(
                "The specified trip cargo does not exist.");

        // 2. Buscar el Trip
        var trip = await _dbContext.Trips
            .FirstOrDefaultAsync(x => x.Id == tripCargo.TripId);

        if (trip == null)
            throw new NotFoundException(
                "The specified trip does not exist.");

        // 3. El Trip debe estar publicado
        if (trip.Status != TripStatus.Published)
            throw new ConflictException(
                "Trip cargo can only be cancelled while the trip is published.");

        // 4. Buscar la solicitud
        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(x =>
                x.Id == tripCargo.TransportRequestId);

        if (request == null)
            throw new NotFoundException(
                "The specified transport request does not exist.");

        // 5. La solicitud debe estar aceptada
        if (request.Status != TransportRequestStatus.Accepted)
            throw new ConflictException(
                "Only accepted transport requests can have a reserved trip cargo cancelled.");

        // 6. Guardar la capacidad antes de cancelar
        var weightKg = tripCargo.WeightKg;
        var volumeM3 = tripCargo.VolumeM3;

        // 7. Cancelar TripCargo
        tripCargo.Cancel();

        // 8. Liberar capacidad del Trip
        trip.ReleaseCapacity(
            weightKg,
            volumeM3);

        // 9. Devolver la solicitud a Published
        request.ReturnToPublished();

        // 10. Guardar todo
        await _dbContext.SaveChangesAsync();
    }
}