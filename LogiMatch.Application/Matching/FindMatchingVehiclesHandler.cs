using LogiMatch.Application;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Matching;

public class FindMatchingVehiclesHandler
{
    private readonly IApplicationDbContext _dbContext;

    public FindMatchingVehiclesHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<object> Handle(
        FindMatchingVehiclesCommand command)
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

        var vehicles = await _dbContext.Vehicles
            .Where(x =>
                x.MaxWeightKg >= totalWeight &&
                x.MaxVolumeM3 >= totalVolume &&
                (!requiresRefrigeration || x.IsRefrigerated) &&
                (!requiresTailLift || x.HasTailLift) &&
                _dbContext.VehicleAvailabilities.Any(a =>
                    a.VehicleId == x.Id &&
                    a.AvailableFrom <= request.PickupDate &&
                    a.AvailableTo >= request.DeliveryDate))
            .Select(x => new
            {
                VehicleId = x.Id,
                x.TransporterProfileId,

                Vehicle = new
                {
                    x.Type,
                    x.Brand,
                    x.Model,
                    x.LicensePlate
                },

                Capacity = new
                {
                    WeightKg = x.MaxWeightKg,
                    VolumeM3 = x.MaxVolumeM3
                },

                Requirements = new
                {
                    HasTailLift = x.HasTailLift,
                    IsRefrigerated = x.IsRefrigerated
                },

                Transporter = _dbContext.TransporterProfiles
                    .Where(tp => tp.Id == x.TransporterProfileId)
                    .Select(tp => new
                    {
                        User = _dbContext.Users
                            .Where(u => u.Id == tp.UserId)
                            .Select(u => new
                            {
                                u.Id,
                                u.FirstName,
                                u.LastName,
                                u.Email,
                                u.Phone
                            })
                            .FirstOrDefault(),

                        Company = tp.CompanyId == null
                            ? null
                            : _dbContext.Companies
                                .Where(c => c.Id == tp.CompanyId)
                                .Select(c => new
                                {
                                    c.Id,
                                    c.Name,
                                    c.TaxId
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
            Matches = vehicles
        };
    }
}