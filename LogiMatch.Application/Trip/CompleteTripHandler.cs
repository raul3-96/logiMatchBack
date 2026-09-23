using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Trips;

public class CompleteTripHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CompleteTripHandler(
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

        if (trip.Status != TripStatus.InProgress)
            throw new ConflictException(
                "Only in-progress trips can be completed.");

        var hasActiveCargo = await _dbContext.TripCargos
            .AnyAsync(x =>
                x.TripId == tripId &&
                (x.Status == TripCargoStatus.Reserved ||
                 x.Status == TripCargoStatus.InProgress));

        if (hasActiveCargo)
            throw new ConflictException(
                "The trip cannot be completed because it has active cargo.");

        trip.Complete();

        await _dbContext.SaveChangesAsync();
    }
}