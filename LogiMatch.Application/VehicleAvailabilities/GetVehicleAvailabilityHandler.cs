using LogiMatch.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.VehicleAvailabilities;

public class GetVehicleAvailabilityHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetVehicleAvailabilityHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<object?> Handle(Guid id)
    {
        var currentUserId = _currentUserService.UserId;

        var availability = await _dbContext.VehicleAvailabilities
            .Where(x =>
                x.Id == id &&
                _dbContext.Vehicles
                    .Where(v => v.Id == x.VehicleId)
                    .Any(v =>
                        _dbContext.TransporterProfiles
                            .Any(tp =>
                                tp.Id == v.TransporterProfileId &&
                                tp.UserId == currentUserId)))
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
        var currentUserId = _currentUserService.UserId;

        var query = _dbContext.VehicleAvailabilities
            .Where(x =>
                _dbContext.Vehicles
                    .Where(v => v.Id == x.VehicleId)
                    .Any(v =>
                        _dbContext.TransporterProfiles
                            .Any(tp =>
                                tp.Id == v.TransporterProfileId &&
                                tp.UserId == currentUserId)));

        if (vehicleId.HasValue)
        {
            query = query.Where(x =>
                x.VehicleId == vehicleId.Value);
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