using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Matching;

public class StartTripCargoHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public StartTripCargoHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
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

        // Solo el transportista dueño del trip puede iniciar un trip cargo
        var transporterProfile = await _dbContext.TransporterProfiles
            .FirstOrDefaultAsync(tp => tp.Id == trip.TransporterProfileId);

        if (transporterProfile == null || transporterProfile.UserId != _currentUserService.UserId)
            throw new ConflictException(
                "You do not have permission to start this trip cargo.");

        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(x => x.Id == tripCargo.TransportRequestId);

        if (request == null)
            throw new NotFoundException(
                "The transport request associated with the trip cargo does not exist.");

        if (trip.Status != TripStatus.InProgress)
            throw new ConflictException(
                "The trip must be in progress before starting trip cargo.");

        tripCargo.Start();
        request.Start();

        await _dbContext.SaveChangesAsync();
    }
}