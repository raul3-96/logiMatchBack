using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Bookings;

public class CompleteBookingHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CompleteBookingHandler(
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

        if (booking.Status != BookingStatus.InProgress)
            throw new ConflictException(
                "Only in-progress bookings can be completed.");

        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(x =>
                x.Id == booking.TransportRequestId);

        if (request == null)
            throw new NotFoundException(
                "The transport request associated with the booking does not exist.");

        if (request.CustomerId != _currentUserService.UserId)
            throw new ConflictException(
                "Only the owner of the transport request can complete the booking.");

        if (request.Status != TransportRequestStatus.InProgress)
            throw new ConflictException(
                "Only in-progress transport requests can be completed.");

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
                "Only accepted transport offers can be completed.");

        booking.Complete();
        request.Complete();

        await _dbContext.SaveChangesAsync();
    }
}