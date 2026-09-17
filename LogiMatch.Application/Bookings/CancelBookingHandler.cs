using LogiMatch.Application;
using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Domain;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Bookings;

public class CancelBookingHandler
{
    private readonly IApplicationDbContext _dbContext;

    public CancelBookingHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(Guid bookingId)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var booking = await _dbContext.Bookings
                .FirstOrDefaultAsync(x => x.Id == bookingId);

            if (booking == null)
                throw new NotFoundException(
                    "The specified booking does not exist.");

            if (booking.Status != BookingStatus.Confirmed)
                throw new ConflictException(
                    "Only confirmed bookings can be cancelled.");

            var request = await _dbContext.TransportRequests
                .FirstOrDefaultAsync(x =>
                    x.Id == booking.TransportRequestId);

            if (request == null)
                throw new NotFoundException(
                    "The transport request associated with the booking does not exist.");

            if (request.Status != TransportRequestStatus.Accepted)
                throw new ConflictException(
                    "Only accepted transport requests can have their booking cancelled.");

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
                    "Only accepted transport offers can have a booking cancelled.");

            booking.Cancel();
            request.ReturnToPublished();

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}