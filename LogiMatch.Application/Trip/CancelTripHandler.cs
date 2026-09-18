using LogiMatch.Application;
using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Domain;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Trips;

public class CancelTripHandler
{
    private readonly IApplicationDbContext _dbContext;

    public CancelTripHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(Guid tripId)
    {
        // 1. Buscar el viaje
        var trip = await _dbContext.Trips
            .FirstOrDefaultAsync(x => x.Id == tripId);

        if (trip == null)
            throw new InvalidOperationException(
                "The specified trip does not exist.");

        // 2. El viaje solo puede cancelarse mientras está publicado
        if (trip.Status != TripStatus.Published)
            throw new InvalidOperationException(
                "Only published trips can be cancelled.");

        // 3. Buscar las cargas activas
        var activeCargos = await _dbContext.TripCargos
            .Where(x =>
                x.TripId == tripId &&
                x.Status != TripCargoStatus.Cancelled)
            .ToListAsync();

        // 4. No puede haber cargas en curso
        if (activeCargos.Any(x =>
            x.Status == TripCargoStatus.InProgress))
        {
            throw new InvalidOperationException(
                "The trip cannot be cancelled because it has cargo in progress.");
        }

        // 5. Todas las cargas activas deben estar reservadas
        if (activeCargos.Any(x =>
            x.Status != TripCargoStatus.Reserved))
        {
            throw new InvalidOperationException(
                "The trip contains cargo with an invalid status for cancellation.");
        }

        // 6. Obtener las solicitudes asociadas
        var requestIds = activeCargos
            .Select(x => x.TransportRequestId)
            .Distinct()
            .ToList();

        var requests = await _dbContext.TransportRequests
            .Where(x => requestIds.Contains(x.Id))
            .ToListAsync();

        // 7. Comprobar que todas las solicitudes existen y están aceptadas
        foreach (var tripCargo in activeCargos)
        {
            var request = requests
                .FirstOrDefault(x =>
                    x.Id == tripCargo.TransportRequestId);

            if (request == null)
                throw new InvalidOperationException(
                    "The transport request associated with the trip cargo does not exist.");

            if (request.Status != TransportRequestStatus.Accepted)
                throw new InvalidOperationException(
                    "A reserved trip cargo must belong to an accepted transport request.");
        }

        // 8. Comprobar que ninguna solicitud tiene un Booking activo
        var activeBookingExists = await _dbContext.Bookings
            .AnyAsync(booking =>
                requestIds.Contains(booking.TransportRequestId) &&
                booking.Status != BookingStatus.Cancelled);

        if (activeBookingExists)
            throw new InvalidOperationException(
                "The trip cannot be cancelled because one of its transport requests has an active booking.");

        // 9. Cancelar las cargas y liberar capacidad
        foreach (var tripCargo in activeCargos)
        {
            var request = requests
                .First(x =>
                    x.Id == tripCargo.TransportRequestId);

            var weightKg = tripCargo.WeightKg;
            var volumeM3 = tripCargo.VolumeM3;

            tripCargo.Cancel();

            trip.ReleaseCapacity(
                weightKg,
                volumeM3);

            request.ReturnToPublished();
        }

        // 10. Cancelar el viaje
        trip.Cancel();

        // 11. Guardar todo
        await _dbContext.SaveChangesAsync();
    }
}