using LogiMatch.Application;
using LogiMatch.Domain;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Matching;

public class FindMatchingTripsHandler
{
    private readonly IApplicationDbContext _dbContext;

    public FindMatchingTripsHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<object> Handle(
        FindMatchingTripsCommand command)
    {
        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(x => x.Id == command.TransportRequestId);

        if (request == null)
            throw new InvalidOperationException(
                "The specified transport request does not exist.");

        var cargos = await _dbContext.Cargos
            .Where(x => x.TransportRequestId == request.Id)
            .ToListAsync();

        if (!cargos.Any())
            throw new InvalidOperationException(
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
                    (!requiresTailLift || v.HasTailLift)
                )
            )
            .Select(t => new
            {
                TripId = t.Id,

                t.TransporterProfileId,
                t.VehicleId,

                t.DepartureDate,
                t.EstimatedArrivalDate,

                AvailableWeightKg = t.AvailableWeightKg,
                AvailableVolumeM3 = t.AvailableVolumeM3,

                RemainingWeightKg =
                    t.AvailableWeightKg - totalWeight,

                RemainingVolumeM3 =
                    t.AvailableVolumeM3 - totalVolume,

                Vehicle = _dbContext.Vehicles
                    .Where(v => v.Id == t.VehicleId)
                    .Select(v => new
                    {
                        v.Type,
                        v.Brand,
                        v.Model,
                        v.LicensePlate,
                        v.MaxWeightKg,
                        v.MaxVolumeM3,
                        v.HasTailLift,
                        v.IsRefrigerated
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