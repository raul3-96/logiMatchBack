using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Trips;

public class StartTripHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public StartTripHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
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

        var currentUserId = _currentUserService.UserId;

        if (transporterProfile.UserId != currentUserId)
        {
            if (!transporterProfile.CompanyId.HasValue)
                throw new ConflictException(
                    "The trip does not belong to the current user.");

            var company = await _dbContext.Companies
                .SingleOrDefaultAsync(x =>
                    x.Id == transporterProfile.CompanyId.Value);

            if (company == null)
                throw new NotFoundException(
                    "The company associated with the transporter profile does not exist.");

            var isOwner = company.OwnerUserId == currentUserId;

            var isAdmin = await _dbContext.CompanyMembers
                .AnyAsync(x =>
                    x.CompanyId == company.Id &&
                    x.UserId == currentUserId &&
                    x.IsActive &&
                    x.Role == CompanyMemberRole.Admin);

            if (!isOwner && !isAdmin)
                throw new ConflictException(
                    "Only the company owner or an administrator can manage this trip.");
        }

        var tripCargos = await _dbContext.TripCargos
            .Where(x => x.TripId == tripId)
            .ToListAsync();

        var activeTripCargos = tripCargos
            .Where(x => x.Status == TripCargoStatus.Reserved)
            .ToList();

        var requestIds = activeTripCargos
            .Select(x => x.TransportRequestId)
            .Distinct()
            .ToList();

        var requests = await _dbContext.TransportRequests
            .Where(x => requestIds.Contains(x.Id))
            .ToListAsync();

        foreach (var tripCargo in activeTripCargos)
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

        trip.Start();

        foreach (var tripCargo in activeTripCargos)
        {
            var request = requests
                .First(x =>
                    x.Id == tripCargo.TransportRequestId);

            tripCargo.Start();
            request.Start();
        }

        await _dbContext.SaveChangesAsync();
    }
}