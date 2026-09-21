using LogiMatch.Application.Common.Exceptions;
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
        var transporterProfile = await _dbContext.TransporterProfiles
            .FirstOrDefaultAsync(x => x.Id == command.TransporterProfileId);

        if (transporterProfile == null)
            throw new NotFoundException(
                "The specified transporter profile does not exist.");

        // Validar que el usuario actual es el dueño del perfil de transportista
        if (transporterProfile.UserId != _currentUserService.UserId)
            throw new ConflictException(
                "You can only create vehicles for your own transporter profile.");

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