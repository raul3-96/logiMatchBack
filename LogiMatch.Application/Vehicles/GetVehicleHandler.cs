using LogiMatch.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Vehicles;

public class GetVehicleHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetVehicleHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<object?> Handle(Guid id)
    {
        var currentUserId = _currentUserService.UserId;

        var vehicle = await _dbContext.Vehicles
            .Where(x =>
                x.Id == id &&
                _dbContext.TransporterProfiles
                    .Any(tp =>
                        tp.Id == x.TransporterProfileId &&
                        tp.UserId == currentUserId))
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

    public async Task<object> HandleAll(
        Guid? transporterProfileId = null)
    {
        var currentUserId = _currentUserService.UserId;

        var query = _dbContext.Vehicles
            .Where(x =>
                _dbContext.TransporterProfiles
                    .Any(tp =>
                        tp.Id == x.TransporterProfileId &&
                        tp.UserId == currentUserId));

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