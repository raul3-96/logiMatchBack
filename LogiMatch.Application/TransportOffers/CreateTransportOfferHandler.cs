using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.TransportOffers;

public class CreateTransportOfferHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateTransportOfferHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        CreateTransportOfferCommand command)
    {
        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(x => x.Id == command.TransportRequestId);

        if (request == null)
            throw new NotFoundException(
                "The specified transport request does not exist.");

        var transporterProfile = await _dbContext.TransporterProfiles
            .FirstOrDefaultAsync(x => x.Id == command.TransporterProfileId);

        if (transporterProfile == null)
            throw new NotFoundException(
                "The specified transporter profile does not exist.");

        // Validar que el usuario actual es el dueño del perfil de transportista
        if (transporterProfile.UserId != _currentUserService.UserId)
            throw new ValidationException(
                "You can only create offers from your own transporter profile.");

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.Id == command.VehicleId);

        if (vehicle == null)
            throw new NotFoundException(
                "The specified vehicle does not exist.");

        if (vehicle.TransporterProfileId != command.TransporterProfileId)
            throw new ValidationException(
                "The vehicle does not belong to the specified transporter.");

        if (command.Price <= 0)
            throw new ValidationException(
                "The offer price must be greater than zero.");

        if (command.EstimatedPickupDate < DateTime.UtcNow)
            throw new ValidationException(
                "Estimated pickup date cannot be in the past.");

        if (command.EstimatedDeliveryDate <= command.EstimatedPickupDate)
            throw new ValidationException(
                "Estimated delivery date must be after estimated pickup date.");

        if (command.EstimatedPickupDate < request.PickupDate)
            throw new ValidationException(
                "Estimated pickup date cannot be before the requested pickup date.");

        if (request.DeliveryDate.HasValue &&
            command.EstimatedDeliveryDate > request.DeliveryDate.Value)
        {
            throw new ValidationException(
                "Estimated delivery date cannot be after the requested delivery date.");
        }

        if (request.Status != TransportRequestStatus.Published &&
            request.Status != TransportRequestStatus.Matching &&
            request.Status != TransportRequestStatus.OffersReceived)
        {
            throw new ValidationException(
                "Offers cannot be created for the current transport request status.");
        }

        var activeTripCargo = await _dbContext.TripCargos
            .AnyAsync(x =>
                x.TransportRequestId == request.Id &&
                x.Status != TripCargoStatus.Cancelled);

        if (activeTripCargo)
            throw new ValidationException(
                "The transport request is already assigned to a trip.");

        var cargos = await _dbContext.Cargos
            .Where(x => x.TransportRequestId == request.Id)
            .ToListAsync();

        if (!cargos.Any())
            throw new NotFoundException(
                "The transport request has no cargo.");

        var totalWeight = cargos.Sum(x => x.WeightKg);
        var totalVolume = cargos.Sum(x => x.VolumeM3);

        var requiresRefrigeration =
            cargos.Any(x => x.RequiresRefrigeration);

        var requiresTailLift =
            cargos.Any(x => x.RequiresTailLift);

        if (totalWeight > vehicle.MaxWeightKg)
            throw new ValidationException(
                "The vehicle does not have enough weight capacity for the transport request.");

        if (totalVolume > vehicle.MaxVolumeM3)
            throw new ValidationException(
                "The vehicle does not have enough volume capacity for the transport request.");

        if (requiresRefrigeration && !vehicle.IsRefrigerated)
            throw new ValidationException(
                "The vehicle does not meet the refrigeration requirement.");

        if (requiresTailLift && !vehicle.HasTailLift)
            throw new ValidationException(
                "The vehicle does not meet the tail lift requirement.");

        var vehicleAvailable = await _dbContext.VehicleAvailabilities
            .AnyAsync(x =>
                x.VehicleId == command.VehicleId &&
                x.AvailableFrom <= command.EstimatedPickupDate &&
                x.AvailableTo >= command.EstimatedDeliveryDate);

        if (!vehicleAvailable)
            throw new ValidationException(
                "The vehicle is not available for the estimated offer dates.");

        var overlappingTrip = await _dbContext.Trips
            .AnyAsync(x =>
                x.VehicleId == command.VehicleId &&
                x.Status != TripStatus.Cancelled &&
                command.EstimatedPickupDate < x.EstimatedArrivalDate &&
                command.EstimatedDeliveryDate > x.DepartureDate);

        if (overlappingTrip)
            throw new ValidationException(
                "The vehicle already has a trip that overlaps with the specified offer dates.");

        var existingOffer = await _dbContext.TransportOffers
            .AnyAsync(x =>
                x.TransportRequestId == command.TransportRequestId &&
                x.TransporterProfileId == command.TransporterProfileId &&
                x.Status == TransportOfferStatus.Pending);

        if (existingOffer)
            throw new ValidationException(
                "The transporter already has a pending offer for this transport request.");

        var acceptedOfferWithActiveBooking =
            await _dbContext.TransportOffers
                .Where(x =>
                    x.TransportRequestId == command.TransportRequestId &&
                    x.TransporterProfileId == command.TransporterProfileId &&
                    x.Status == TransportOfferStatus.Accepted)
                .Join(
                    _dbContext.Bookings,
                    offer => offer.Id,
                    booking => booking.TransportOfferId,
                    (offer, booking) => booking)
                .AnyAsync(x => x.Status != BookingStatus.Cancelled);

        if (acceptedOfferWithActiveBooking)
            throw new ValidationException(
                "The transporter already has an active booking for this transport request.");

        var offer = new TransportOffer(
            command.TransportRequestId,
            command.TransporterProfileId,
            command.VehicleId,
            command.Price,
            command.EstimatedPickupDate,
            command.EstimatedDeliveryDate);

        _dbContext.TransportOffers.Add(offer);

        if (request.Status == TransportRequestStatus.Published ||
            request.Status == TransportRequestStatus.Matching)
        {
            request.MarkOffersReceived();
        }

        await _dbContext.SaveChangesAsync();

        return offer.Id;
    }
}