using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Trips;

public class CancelTripHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITripManagementAccessService _tripManagementAccessService;

    public CancelTripHandler(
        IApplicationDbContext dbContext,
        ITripManagementAccessService tripManagementAccessService)
    {
        _dbContext = dbContext;
        _tripManagementAccessService = tripManagementAccessService;
    }

    public async Task Handle(Guid tripId)
    {
        var trip = await _dbContext.Trips
            .FirstOrDefaultAsync(x => x.Id == tripId);

        if (trip == null)
            throw new NotFoundException(
                "The specified trip does not exist.");

        var transporterProfile = await _dbContext.TransporterProfiles
            .FirstOrDefaultAsync(x => x.Id == trip.TransporterProfileId);

        if (transporterProfile == null)
            throw new NotFoundException(
                "The transporter profile associated with the trip does not exist.");

        var allowedProfileIds =
            await _tripManagementAccessService
                .GetManageableTransporterProfileIdsAsync();

        if (!allowedProfileIds.Contains(transporterProfile.Id))
            throw new ConflictException(
                "Only the company owner, an administrator, or the profile owner can manage this trip.");

        if (trip.Status != TripStatus.Published)
            throw new ConflictException(
                "Only published trips can be cancelled.");

        var activeCargos = await _dbContext.TripCargos
            .Where(x =>
                x.TripId == tripId &&
                x.Status != TripCargoStatus.Cancelled)
            .ToListAsync();

        if (activeCargos.Any(x =>
            x.Status == TripCargoStatus.InProgress))
        {
            throw new ConflictException(
                "The trip cannot be cancelled because it has cargo in progress.");
        }

        if (activeCargos.Any(x =>
            x.Status != TripCargoStatus.Reserved))
        {
            throw new ConflictException(
                "The trip contains cargo with an invalid status for cancellation.");
        }

        var requestIds = activeCargos
            .Select(x => x.TransportRequestId)
            .Distinct()
            .ToList();

        var requests = await _dbContext.TransportRequests
            .Where(x => requestIds.Contains(x.Id))
            .ToListAsync();

        foreach (var tripCargo in activeCargos)
        {
            var request = requests
                .FirstOrDefault(x =>
                    x.Id == tripCargo.TransportRequestId);

            if (request == null)
                throw new NotFoundException(
                    "The transport request associated with the trip cargo does not exist.");

            if (request.Status != TransportRequestStatus.Accepted)
                throw new ConflictException(
                    "A reserved trip cargo must belong to an accepted transport request.");
        }

        var activeBookingExists = await _dbContext.Bookings
            .AnyAsync(booking =>
                requestIds.Contains(booking.TransportRequestId) &&
                booking.Status != BookingStatus.Cancelled);

        if (activeBookingExists)
            throw new ConflictException(
                "The trip cannot be cancelled because one of its transport requests has an active booking.");

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

        trip.Cancel();

        await _dbContext.SaveChangesAsync();
    }
}