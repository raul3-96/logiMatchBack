using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Matching;

public class FindMatchingTripsHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public FindMatchingTripsHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<object> Handle(
        FindMatchingTripsCommand command)
    {
        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(x => x.Id == command.TransportRequestId);

        if (request == null)
            throw new NotFoundException(
                "The specified transport request does not exist.");

        if (request.CustomerId != _currentUserService.UserId)
            throw new ConflictException(
                "You do not have permission to search trips for this transport request.");

        var cargos = await _dbContext.Cargos
            .Where(x => x.TransportRequestId == request.Id)
            .ToListAsync();

        if (!cargos.Any())
            throw new ConflictException(
                "The transport request has no cargo.");

        var totalWeight = cargos.Sum(x => x.WeightKg);
        var totalVolume = cargos.Sum(x => x.VolumeM3);

        var requiresRefrigeration =
            cargos.Any(x => x.RequiresRefrigeration);

        var requiresTailLift =
            cargos.Any(x => x.RequiresTailLift);

        var matches = await _dbContext.Trips
            .Where(t =>
                t.Status == TripStatus.Published &&
                t.OriginLocationId == request.PickupLocationId &&
                t.DestinationLocationId == request.DeliveryLocationId &&
                t.DepartureDate <= request.PickupDate &&
                t.EstimatedArrivalDate >= request.PickupDate &&
                (!request.DeliveryDate.HasValue ||
                 t.EstimatedArrivalDate <= request.DeliveryDate.Value) &&
                t.AvailableWeightKg >= totalWeight &&
                t.AvailableVolumeM3 >= totalVolume &&
                _dbContext.Vehicles.Any(v =>
                    v.Id == t.VehicleId &&
                    (!requiresRefrigeration || v.IsRefrigerated) &&
                    (!requiresTailLift || v.HasTailLift)))
            .Select(t => new
            {
                TripId = t.Id,
                t.TransporterProfileId,
                t.DepartureDate,
                t.EstimatedArrivalDate,
                AvailableWeightKg = t.AvailableWeightKg,
                AvailableVolumeM3 = t.AvailableVolumeM3,
                RemainingWeightKg = t.AvailableWeightKg - totalWeight,
                RemainingVolumeM3 = t.AvailableVolumeM3 - totalVolume,

                Vehicle = _dbContext.Vehicles
                    .Where(v => v.Id == t.VehicleId)
                    .Select(v => new
                    {
                        v.Type,
                        v.Brand,
                        v.Model,
                        v.MaxWeightKg,
                        v.MaxVolumeM3,
                        v.HasTailLift,
                        v.IsRefrigerated
                    })
                    .FirstOrDefault(),

                Transporter = _dbContext.TransporterProfiles
                    .Where(tp => tp.Id == t.TransporterProfileId)
                    .Select(tp => new
                    {
                        tp.Id,

                        User = _dbContext.Users
                            .Where(u => u.Id == tp.UserId)
                            .Select(u => new
                            {
                                u.FirstName,
                                u.LastName
                            })
                            .FirstOrDefault(),

                        Company = tp.CompanyId == null
                            ? null
                            : _dbContext.Companies
                                .Where(c => c.Id == tp.CompanyId)
                                .Select(c => new
                                {
                                    c.Name
                                })
                                .FirstOrDefault()
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return new
        {
            TransportRequestId = request.Id,
            RequiredWeightKg = totalWeight,
            RequiredVolumeM3 = totalVolume,
            RequiresRefrigeration = requiresRefrigeration,
            RequiresTailLift = requiresTailLift,
            Matches = matches
        };
    }
}