using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.VehicleAvailabilities;

public class GetVehicleAvailabilityHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetVehicleAvailabilityHandler(
        IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<object?> Handle(Guid id)
    {
        var availability = await _dbContext.VehicleAvailabilities
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.VehicleId,
                x.AvailableFrom,
                x.AvailableTo
            })
            .FirstOrDefaultAsync();

        return availability;
    }

    public async Task<object> HandleAll(Guid? vehicleId = null)
    {
        var query = _dbContext.VehicleAvailabilities.AsQueryable();

        if (vehicleId.HasValue)
        {
            query = query.Where(x => x.VehicleId == vehicleId.Value);
        }

        var availabilities = await query
            .Select(x => new
            {
                x.Id,
                x.VehicleId,
                x.AvailableFrom,
                x.AvailableTo
            })
            .OrderBy(x => x.AvailableFrom)
            .ThenBy(x => x.AvailableTo)
            .ToListAsync();

        return availabilities;
    }
}