using LogiMatch.Application;
using LogiMatch.Domain;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.TransportRequests;

public class CancelTransportRequestHandler
{
    private readonly IApplicationDbContext _dbContext;

    public CancelTransportRequestHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(Guid transportRequestId)
    {
        // 1. Buscar la solicitud
        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(x => x.Id == transportRequestId);

        if (request == null)
            throw new InvalidOperationException(
                "The specified transport request does not exist.");

        // 2. Comprobar que la solicitud puede cancelarse
        if (request.Status == TransportRequestStatus.InProgress)
            throw new InvalidOperationException(
                "In-progress transport requests cannot be cancelled.");

        if (request.Status == TransportRequestStatus.Completed)
            throw new InvalidOperationException(
                "Completed transport requests cannot be cancelled.");

        if (request.Status == TransportRequestStatus.Cancelled)
            throw new InvalidOperationException(
                "The transport request is already cancelled.");

        if (request.Status == TransportRequestStatus.Expired)
            throw new InvalidOperationException(
                "Expired transport requests cannot be cancelled.");

        // 3. Comprobar que no existe un Booking activo
        var activeBooking = await _dbContext.Bookings
            .AnyAsync(x =>
                x.TransportRequestId == transportRequestId &&
                x.Status != BookingStatus.Cancelled);

        if (activeBooking)
            throw new InvalidOperationException(
                "The transport request has an active booking and must be cancelled through the booking.");

        // 4. Buscar un TripCargo activo
        var tripCargo = await _dbContext.TripCargos
            .FirstOrDefaultAsync(x =>
                x.TransportRequestId == transportRequestId &&
                x.Status != TripCargoStatus.Cancelled);

        if (tripCargo != null)
        {
            // 5. Buscar el Trip
            var trip = await _dbContext.Trips
                .FirstOrDefaultAsync(x =>
                    x.Id == tripCargo.TripId);

            if (trip == null)
                throw new InvalidOperationException(
                    "The trip associated with the transport request does not exist.");

            // 6. Solo se puede cancelar mientras el Trip esté publicado
            if (trip.Status != TripStatus.Published)
                throw new InvalidOperationException(
                    "A transport request with a reserved trip cargo can only be cancelled while the trip is published.");

            // 7. Guardar la capacidad antes de cancelar el TripCargo
            var weightKg = tripCargo.WeightKg;
            var volumeM3 = tripCargo.VolumeM3;

            // 78 Cancelar TripCargo
            tripCargo.Cancel();

            // 9. Liberar capacidad
            trip.ReleaseCapacity(
                weightKg,
                volumeM3);
        }

        // 10. Cancelar la solicitud
        request.Cancel();

        // 11. Guardar todo
        await _dbContext.SaveChangesAsync();
    }
}