using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.VehicleAvailabilities;

public class CreateVehicleAvailabilityHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateVehicleAvailabilityHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        CreateVehicleAvailabilityCommand command)
    {
        var currentUserId = _currentUserService.UserId;

        var vehicle = await _dbContext.Vehicles
            .Where(x => x.Id == command.VehicleId)
            .Join(
                _dbContext.TransporterProfiles,
                vehicle => vehicle.TransporterProfileId,
                profile => profile.Id,
                (vehicle, profile) => new
                {
                    Vehicle = vehicle,
                    profile.UserId
                })
            .SingleOrDefaultAsync();

        if (vehicle == null)
            throw new NotFoundException(
                "The specified vehicle does not exist.");

        if (vehicle.UserId != currentUserId)
            throw new ValidationException(
                "The vehicle does not belong to the authenticated user.");

        //overlapping availability check
        var overlappingAvailability = await _dbContext.VehicleAvailabilities
            .Where(x => x.VehicleId == command.VehicleId)
            .Where(x => x.AvailableFrom < command.AvailableTo && x.AvailableTo > command.AvailableFrom)
            .AnyAsync();
        if (overlappingAvailability)
            throw new ValidationException(
                "The specified availability overlaps with an existing availability for the vehicle.");

        var availability = new VehicleAvailability(
            command.VehicleId,
            command.AvailableFrom,
            command.AvailableTo);

        _dbContext.VehicleAvailabilities.Add(availability);

        await _dbContext.SaveChangesAsync();

        return availability.Id;
    }
}