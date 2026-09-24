using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Matching;

public class CompleteTripCargoHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITripManagementAccessService _tripManagementAccessService;

    public CompleteTripCargoHandler(
        IApplicationDbContext dbContext,
        ITripManagementAccessService tripManagementAccessService)
    {
        _dbContext = dbContext;
        _tripManagementAccessService = tripManagementAccessService;
    }

    public async Task Handle(Guid tripCargoId)
    {
        var tripCargo = await _dbContext.TripCargos
            .FirstOrDefaultAsync(x => x.Id == tripCargoId);

        if (tripCargo == null)
            throw new NotFoundException(
                "The specified trip cargo does not exist.");

        var trip = await _dbContext.Trips
            .FirstOrDefaultAsync(x => x.Id == tripCargo.TripId);

        if (trip == null)
            throw new NotFoundException(
                "The trip associated with the trip cargo does not exist.");

        var transporterProfile = await _dbContext.TransporterProfiles
            .FirstOrDefaultAsync(tp => tp.Id == trip.TransporterProfileId);

        if (transporterProfile == null)
            throw new NotFoundException(
                "The transporter profile associated with the trip does not exist.");

        var allowedProfileIds =
            await _tripManagementAccessService
                .GetManageableTransporterProfileIdsAsync();

        if (!allowedProfileIds.Contains(transporterProfile.Id))
            throw new ConflictException(
                "Only the company owner, an administrator, or the profile owner can manage this trip cargo.");

        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(x => x.Id == tripCargo.TransportRequestId);

        if (request == null)
            throw new NotFoundException(
                "The transport request associated with the trip cargo does not exist.");

        if (trip.Status != TripStatus.InProgress)
            throw new ConflictException(
                "The trip must be in progress before completing trip cargo.");

        tripCargo.Complete();
        request.Complete();

        await _dbContext.SaveChangesAsync();
    }
}