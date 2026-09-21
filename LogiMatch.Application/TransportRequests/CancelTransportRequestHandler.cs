using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.TransportRequests;

public class CancelTransportRequestHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CancelTransportRequestHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(Guid transportRequestId)
    {
        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(x => x.Id == transportRequestId);

        if (request == null)
            throw new NotFoundException(
                "The specified transport request does not exist.");

        // Validar que el usuario actual es el dueño de la solicitud
        if (request.CustomerId != _currentUserService.UserId)
            throw new ConflictException(
                "You can only cancel your own transport requests.");

        // Comprobar que la solicitud puede cancelarse
        if (request.Status == TransportRequestStatus.InProgress)
            throw new ConflictException(
                "In-progress transport requests cannot be cancelled.");

        if (request.Status == TransportRequestStatus.Completed)
            throw new ConflictException(
                "Completed transport requests cannot be cancelled.");

        if (request.Status == TransportRequestStatus.Cancelled)
            throw new ConflictException(
                "The transport request is already cancelled.");

        if (request.Status == TransportRequestStatus.Expired)
            throw new ConflictException(
                "Expired transport requests cannot be cancelled.");

        // Comprobar que no existe un Booking activo
        var activeBooking = await _dbContext.Bookings
            .AnyAsync(x =>
                x.TransportRequestId == transportRequestId &&
                x.Status != BookingStatus.Cancelled);

        if (activeBooking)
            throw new ConflictException(
                "The transport request has an active booking and must be cancelled through the booking.");

        // Buscar un TripCargo activo
        var tripCargo = await _dbContext.TripCargos
            .FirstOrDefaultAsync(x =>
                x.TransportRequestId == transportRequestId &&
                x.Status != TripCargoStatus.Cancelled);

        if (tripCargo != null)
        {
            // Buscar el Trip
            var trip = await _dbContext.Trips
                .FirstOrDefaultAsync(x => x.Id == tripCargo.TripId);

            if (trip == null)
                throw new NotFoundException(
                    "The trip associated with the transport request does not exist.");

            // Solo se puede cancelar mientras el Trip esté publicado
            if (trip.Status != TripStatus.Published)
                throw new ConflictException(
                    "A transport request with a reserved trip cargo can only be cancelled while the trip is published.");

            // Guardar la capacidad antes de cancelar el TripCargo
            var weightKg = tripCargo.WeightKg;
            var volumeM3 = tripCargo.VolumeM3;

            // Cancelar TripCargo
            tripCargo.Cancel();

            // Liberar capacidad
            trip.ReleaseCapacity(weightKg, volumeM3);
        }

        // Cancelar la solicitud
        request.Cancel();

        // Guardar todo
        await _dbContext.SaveChangesAsync();
    }
}