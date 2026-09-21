using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Matching;

public class ReserveTripCapacityHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public ReserveTripCapacityHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        ReserveTripCapacityCommand command)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var trip = await GetTripForReservation(command.TripId);

            if (trip == null)
                throw new NotFoundException(
                    "The specified trip does not exist.");

            if (trip.Status != TripStatus.Published)
                throw new ConflictException(
                    "Only published trips can reserve capacity.");

            var request = await _dbContext.TransportRequests
                .FirstOrDefaultAsync(x =>
                    x.Id == command.TransportRequestId);

            if (request == null)
                throw new NotFoundException(
                    "The specified transport request does not exist.");

            if (request.CustomerId != _currentUserService.UserId)
                throw new ConflictException(
                    "You do not have permission to reserve this transport request.");

            if (request.Status != TransportRequestStatus.Published &&
                request.Status != TransportRequestStatus.Matching)
            {
                throw new ConflictException(
                    "Only published or matching transport requests can be assigned to a trip.");
            }

            var existingReservation = await _dbContext.TripCargos
                .AnyAsync(x =>
                    x.TransportRequestId == request.Id &&
                    x.Status != TripCargoStatus.Cancelled);

            if (existingReservation)
                throw new ConflictException(
                    "The transport request is already reserved on another trip.");

            var cargos = await _dbContext.Cargos
                .Where(x => x.TransportRequestId == request.Id)
                .ToListAsync();

            if (!cargos.Any())
                throw new ConflictException(
                    "The transport request has no cargo.");

            var totalWeight = cargos.Sum(x => x.WeightKg);
            var totalVolume = cargos.Sum(x => x.VolumeM3);

            var requiresRefrigeration =
                cargos.Any(x => x.RequiresRefrigeration);

            var requiresTailLift =
                cargos.Any(x => x.RequiresTailLift);

            var vehicle = await _dbContext.Vehicles
                .FirstOrDefaultAsync(x => x.Id == trip.VehicleId);

            if (vehicle == null)
                throw new NotFoundException(
                    "The vehicle associated with the trip does not exist.");

            if (requiresRefrigeration && !vehicle.IsRefrigerated)
                throw new ConflictException(
                    "The vehicle does not meet the refrigeration requirement.");

            if (requiresTailLift && !vehicle.HasTailLift)
                throw new ConflictException(
                    "The vehicle does not meet the tail lift requirement.");

            if (trip.OriginLocationId != request.PickupLocationId)
                throw new ConflictException(
                    "The trip origin does not match the transport request pickup location.");

            if (trip.DestinationLocationId != request.DeliveryLocationId)
                throw new ConflictException(
                    "The trip destination does not match the transport request delivery location.");

            if (trip.DepartureDate > request.PickupDate)
                throw new ConflictException(
                    "The trip departure date is after the requested pickup date.");

            if (trip.EstimatedArrivalDate < request.PickupDate)
                throw new ConflictException(
                    "The trip arrives before the requested pickup date.");

            if (request.DeliveryDate.HasValue &&
                trip.EstimatedArrivalDate > request.DeliveryDate.Value)
            {
                throw new ConflictException(
                    "The trip arrival date is after the requested delivery deadline.");
            }

            if (trip.AvailableWeightKg < totalWeight)
                throw new ConflictException(
                    "The trip does not have enough available weight.");

            if (trip.AvailableVolumeM3 < totalVolume)
                throw new ConflictException(
                    "The trip does not have enough available volume.");

            var existing = await _dbContext.TripCargos
                .AnyAsync(x =>
                    x.TripId == trip.Id &&
                    x.TransportRequestId == request.Id &&
                    x.Status != TripCargoStatus.Cancelled);

            if (existing)
                throw new ConflictException(
                    "The transport request is already reserved on this trip.");

            trip.ReserveCapacity(
                totalWeight,
                totalVolume);

            var tripCargo = new TripCargo(
                trip.Id,
                request.Id,
                totalWeight,
                totalVolume);

            _dbContext.TripCargos.Add(tripCargo);

            request.AssignToTrip();

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return tripCargo.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<Trip?> GetTripForReservation(Guid tripId)
    {
        if (_dbContext.SupportsRowLocking)
        {
            return await _dbContext.Trips
                .FromSqlInterpolated($"""
                SELECT *
                FROM "trips"
                WHERE "Id" = {tripId}
                FOR UPDATE
                """)
                .FirstOrDefaultAsync();
        }

        return await _dbContext.Trips
            .FirstOrDefaultAsync(x => x.Id == tripId);
    }
}