using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.VehicleAvailabilities;

public class CreateVehicleAvailabilityHandler
{
    private readonly IApplicationDbContext _dbContext;

    public CreateVehicleAvailabilityHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(
        CreateVehicleAvailabilityCommand command)
    {
        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.Id == command.VehicleId);

        if (vehicle == null)
            throw new NotFoundException(
                "The specified vehicle does not exist.");

        if (command.AvailableTo <= command.AvailableFrom)
            throw new ValidationException(
                "AvailableTo must be after AvailableFrom.");

        if (command.AvailableFrom < DateTime.UtcNow)
            throw new ValidationException(
                "AvailableFrom cannot be in the past.");

        var overlappingAvailability =
            await _dbContext.VehicleAvailabilities
                .AnyAsync(x =>
                    x.VehicleId == command.VehicleId &&
                    command.AvailableFrom < x.AvailableTo &&
                    command.AvailableTo > x.AvailableFrom);

        if (overlappingAvailability)
            throw new ConflictException(
                "The vehicle already has an availability that overlaps with the specified dates.");

        var availability = new VehicleAvailability(
            command.VehicleId,
            command.AvailableFrom,
            command.AvailableTo);

        _dbContext.VehicleAvailabilities.Add(availability);

        await _dbContext.SaveChangesAsync();

        return availability.Id;
    }
}