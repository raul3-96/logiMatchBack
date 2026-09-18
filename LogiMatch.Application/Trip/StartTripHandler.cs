using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Domain;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Trips;

public class StartTripHandler
{
    private readonly IApplicationDbContext _dbContext;

    public StartTripHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(Guid tripId)
    {
        // 1. Buscar el viaje
        var trip = await _dbContext.Trips
            .FirstOrDefaultAsync(x => x.Id == tripId);

        if (trip == null)
            throw new NotFoundException(
                "El viaje especificado no existe.");

        // 2. Buscar las cargas del viaje
        var tripCargos = await _dbContext.TripCargos
            .Where(x => x.TripId == tripId)
            .ToListAsync();

        // 3. Buscar las solicitudes asociadas a las cargas activas
        var activeTripCargos = tripCargos
            .Where(x => x.Status == TripCargoStatus.Reserved)
            .ToList();

        var requestIds = activeTripCargos
            .Select(x => x.TransportRequestId)
            .Distinct()
            .ToList();

        var requests = await _dbContext.TransportRequests
            .Where(x => requestIds.Contains(x.Id))
            .ToListAsync();

        // 4. Comprobar que todas las solicitudes existen
        foreach (var tripCargo in activeTripCargos)
        {
            var request = requests
                .FirstOrDefault(x =>
                    x.Id == tripCargo.TransportRequestId);

            if (request == null)
                throw new NotFoundException(
                    "La solicitud de transporte asociada con la carga del viaje no existe.");

            // 5. La solicitud debe estar Accepted
            if (request.Status != TransportRequestStatus.Accepted)
                throw new ConflictException(
                    "Una carga de viaje reservada debe pertenecer a una solicitud de transporte aceptada.");
        }

        // 6. Iniciar el viaje
        trip.Start();

        // 7. Iniciar las cargas y solicitudes
        foreach (var tripCargo in activeTripCargos)
        {
            var request = requests
                .First(x =>
                    x.Id == tripCargo.TransportRequestId);

            tripCargo.Start();
            request.Start();
        }

        // 8. Guardar todo
        await _dbContext.SaveChangesAsync();
    }
}