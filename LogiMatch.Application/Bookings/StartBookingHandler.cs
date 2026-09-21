using LogiMatch.Application;
using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Bookings;

public class StartBookingHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public StartBookingHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(Guid bookingId)
    {
        var booking = await _dbContext.Bookings
            .FirstOrDefaultAsync(x => x.Id == bookingId);

        if (booking == null)
            throw new NotFoundException(
                "The specified booking does not exist.");

        if (booking.Status != BookingStatus.Confirmed)
            throw new ConflictException(
                "Only confirmed bookings can be started.");

        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(x =>
                x.Id == booking.TransportRequestId);

        if (request == null)
            throw new NotFoundException(
                "The transport request associated with the booking does not exist.");

        if (request.CustomerId != _currentUserService.UserId)
            throw new ConflictException(
                "Only the owner of the transport request can start the booking.");

        if (request.Status != TransportRequestStatus.Accepted)
            throw new ConflictException(
                "Only accepted transport requests can be started.");

        var tripCargoExists = await _dbContext.TripCargos
            .AnyAsync(x =>
                x.TransportRequestId == request.Id &&
                x.Status != TripCargoStatus.Cancelled);

        if (tripCargoExists)
            throw new ConflictException(
                "The transport request is already assigned to a trip.");

        var offer = await _dbContext.TransportOffers
            .FirstOrDefaultAsync(x =>
                x.Id == booking.TransportOfferId);

        if (offer == null)
            throw new NotFoundException(
                "The transport offer associated with the booking does not exist.");

        if (offer.TransportRequestId != request.Id)
            throw new ConflictException(
                "The transport offer does not belong to the transport request.");

        if (offer.Status != TransportOfferStatus.Accepted)
            throw new ConflictException(
                "Only accepted transport offers can be started.");

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x =>
                x.Id == offer.VehicleId);

        if (vehicle == null)
            throw new NotFoundException(
                "The vehicle associated with the transport offer does not exist.");

        if (vehicle.TransporterProfileId != offer.TransporterProfileId)
            throw new ConflictException(
                "The vehicle does not belong to the transporter who created the offer.");

        var vehicleHasActiveTrip = await _dbContext.Trips
            .AnyAsync(x =>
                x.VehicleId == offer.VehicleId &&
                x.Status == TripStatus.InProgress);

        if (vehicleHasActiveTrip)
            throw new ConflictException(
                "The vehicle already has another trip in progress.");

        booking.Start();
        request.Start();

        await _dbContext.SaveChangesAsync();
    }
}