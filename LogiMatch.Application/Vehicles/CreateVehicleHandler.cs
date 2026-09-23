using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Vehicles;

public class CreateVehicleHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateVehicleHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        CreateVehicleCommand command)
    {
        var currentUserId = _currentUserService.UserId;

        var profile = await _dbContext.TransporterProfiles
            .SingleOrDefaultAsync(x => x.Id == command.TransporterProfileId);

        if (profile == null)
            throw new InvalidOperationException(
                "The specified transporter profile does not exist.");

        if (profile.UserId != currentUserId)
            throw new InvalidOperationException(
                "The transporter profile does not belong to the authenticated user.");

        var vehicle = new Vehicle(
            command.TransporterProfileId,
            command.Type,
            command.Brand,
            command.Model,
            command.LicensePlate,
            command.MaxWeightKg,
            command.MaxVolumeM3,
            command.LengthM,
            command.WidthM,
            command.HeightM,
            command.HasTailLift,
            command.IsRefrigerated);

        _dbContext.Vehicles.Add(vehicle);

        await _dbContext.SaveChangesAsync();

        return vehicle.Id;
    }
}