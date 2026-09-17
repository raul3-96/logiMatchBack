using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Trips;

public class CreateTripHandler
{
    private readonly IApplicationDbContext _dbContext;

    public CreateTripHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateTripCommand command)
    {
        var transporterExists = await _dbContext.TransporterProfiles
            .AnyAsync(x => x.Id == command.TransporterProfileId);

        if (!transporterExists)
            throw new NotFoundException(
                "The specified transporter profile does not exist.");

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.Id == command.VehicleId);

        if (vehicle == null)
            throw new NotFoundException(
                "The specified vehicle does not exist.");

        if (vehicle.TransporterProfileId != command.TransporterProfileId)
            throw new ConflictException(
                "The vehicle does not belong to the specified transporter.");

        var originExists = await _dbContext.Locations
            .AnyAsync(x => x.Id == command.OriginLocationId);

        if (!originExists)
            throw new NotFoundException(
                "The specified origin location does not exist.");

        var destinationExists = await _dbContext.Locations
            .AnyAsync(x => x.Id == command.DestinationLocationId);

        if (!destinationExists)
            throw new NotFoundException(
                "The specified destination location does not exist.");

        if (command.OriginLocationId == command.DestinationLocationId)
            throw new ValidationException(
                "Origin and destination locations must be different.");

        if (command.EstimatedArrivalDate <= command.DepartureDate)
            throw new ValidationException(
                "Estimated arrival must be after departure.");

        if (command.DepartureDate < DateTime.UtcNow)
            throw new ValidationException(
                "Departure date cannot be in the past.");

        if (command.AvailableWeightKg <= 0)
            throw new ValidationException(
                "Available weight must be greater than zero.");

        if (command.AvailableVolumeM3 <= 0)
            throw new ValidationException(
                "Available volume must be greater than zero.");

        if (command.AvailableWeightKg > vehicle.MaxWeightKg)
            throw new ValidationException(
                "Available weight cannot exceed vehicle capacity.");

        if (command.AvailableVolumeM3 > vehicle.MaxVolumeM3)
            throw new ValidationException(
                "Available volume cannot exceed vehicle capacity.");

        var vehicleAvailable = await _dbContext.VehicleAvailabilities
            .AnyAsync(x =>
                x.VehicleId == command.VehicleId &&
                x.AvailableFrom <= command.DepartureDate &&
                x.AvailableTo >= command.EstimatedArrivalDate);

        if (!vehicleAvailable)
            throw new ConflictException(
                "The vehicle is not available for the trip dates.");

        var overlappingTrip = await _dbContext.Trips
            .AnyAsync(x =>
                x.VehicleId == command.VehicleId &&
                x.Status != TripStatus.Cancelled &&
                command.DepartureDate < x.EstimatedArrivalDate &&
                command.EstimatedArrivalDate > x.DepartureDate);

        if (overlappingTrip)
            throw new ConflictException(
                "The vehicle already has a trip that overlaps with the specified dates.");

        var overlappingBooking = await _dbContext.Bookings
            .Join(
                _dbContext.TransportOffers,
                booking => booking.TransportOfferId,
                offer => offer.Id,
                (booking, offer) => new { booking, offer })
            .AnyAsync(x =>
                x.booking.Status != BookingStatus.Cancelled &&
                x.offer.VehicleId == command.VehicleId &&
                x.offer.EstimatedPickupDate < command.EstimatedArrivalDate &&
                x.offer.EstimatedDeliveryDate > command.DepartureDate);

        if (overlappingBooking)
            throw new ConflictException(
                "The vehicle already has a booking that overlaps with the specified dates.");

        var trip = new Trip(
            command.TransporterProfileId,
            command.VehicleId,
            command.OriginLocationId,
            command.DestinationLocationId,
            command.DepartureDate,
            command.EstimatedArrivalDate,
            command.AvailableWeightKg,
            command.AvailableVolumeM3);

        _dbContext.Trips.Add(trip);

        // 15. Guardar
        await _dbContext.SaveChangesAsync();

        return trip.Id;
    }
}