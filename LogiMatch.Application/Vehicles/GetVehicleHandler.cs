using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Vehicles;

public class GetVehicleHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetVehicleHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<object?> Handle(Guid id)
    {
        var vehicle = await _dbContext.Vehicles
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.TransporterProfileId,
                x.Type,
                x.Brand,
                x.Model,
                x.LicensePlate,
                x.MaxWeightKg,
                x.MaxVolumeM3,
                x.LengthM,
                x.WidthM,
                x.HeightM,
                x.HasTailLift,
                x.IsRefrigerated
            })
            .FirstOrDefaultAsync();

        return vehicle;
    }

    public async Task<object> HandleAll(Guid? transporterProfileId = null)
    {
        var query = _dbContext.Vehicles.AsQueryable();

        if (transporterProfileId.HasValue)
        {
            query = query.Where(x =>
                x.TransporterProfileId == transporterProfileId.Value);
        }

        var vehicles = await query
            .Select(x => new
            {
                x.Id,
                x.TransporterProfileId,
                x.Type,
                x.Brand,
                x.Model,
                x.LicensePlate,
                x.MaxWeightKg,
                x.MaxVolumeM3,
                x.LengthM,
                x.WidthM,
                x.HeightM,
                x.HasTailLift,
                x.IsRefrigerated
            })
            .OrderBy(x => x.Brand)
            .ThenBy(x => x.Model)
            .ThenBy(x => x.LicensePlate)
            .ToListAsync();

        return vehicles;
    }
}