using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Matching;

public class FindMatchingVehiclesHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public FindMatchingVehiclesHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<object> Handle(
        FindMatchingVehiclesCommand command)
    {
        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(x => x.Id == command.TransportRequestId);

        if (request == null)
            throw new NotFoundException(
                "The specified transport request does not exist.");

        if (request.CustomerId != _currentUserService.UserId)
            throw new ConflictException(
                "You do not have permission to search vehicles for this transport request.");

        var cargos = await _dbContext.Cargos
            .Where(x => x.TransportRequestId == request.Id)
            .ToListAsync();

        if (!cargos.Any())
            throw new ConflictException(
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
                    x.MaxWeightKg,
                    x.MaxVolumeM3,
                    x.HasTailLift,
                    x.IsRefrigerated
                },

                Transporter = _dbContext.TransporterProfiles
                    .Where(tp => tp.Id == x.TransporterProfileId)
                    .Select(tp => new
                    {
                        tp.Id,

                        User = _dbContext.Users
                            .Where(u => u.Id == tp.UserId)
                            .Select(u => new
                            {
                                u.FirstName,
                                u.LastName
                            })
                            .FirstOrDefault(),

                        Company = tp.CompanyId == null
                            ? null
                            : _dbContext.Companies
                                .Where(c => c.Id == tp.CompanyId)
                                .Select(c => new
                                {
                                    c.Name
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