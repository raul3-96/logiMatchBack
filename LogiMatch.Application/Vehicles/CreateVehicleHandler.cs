using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Vehicles;

public class CreateVehicleHandler
{
    private readonly IApplicationDbContext _dbContext;

    public CreateVehicleHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(
        CreateVehicleCommand command)
    {
        var transporterExists = await _dbContext.TransporterProfiles
            .AnyAsync(x => x.Id == command.TransporterProfileId);

        if (!transporterExists)
            throw new NotFoundException(
                "The specified transporter profile does not exist.");

        var licensePlate = command.LicensePlate
            .Trim()
            .ToUpperInvariant();

        var licensePlateExists = await _dbContext.Vehicles
            .AnyAsync(x => x.LicensePlate == licensePlate);

        if (licensePlateExists)
            throw new ConflictException(
                "A vehicle with the specified license plate already exists.");

        var vehicle = new Vehicle(
            command.TransporterProfileId,
            command.Type,
            command.Brand,
            command.Model,
            licensePlate,
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