using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Trips;

public class GetTripHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetTripHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<object?> Handle(GetTripCommand command)
    {
        var trip = await _dbContext.Trips
            .Where(x => x.Id == command.TripId)
            .Select(x => new
            {
                x.Id,
                x.TransporterProfileId,
                x.VehicleId,
                x.DepartureDate,
                x.EstimatedArrivalDate,
                x.AvailableWeightKg,
                x.AvailableVolumeM3,
                x.Status,

                Origin = _dbContext.Locations
                    .Where(l => l.Id == x.OriginLocationId)
                    .Select(l => new
                    {
                        l.Id,
                        l.Address,
                        l.City,
                        l.PostalCode,
                        l.Country,
                        l.Latitude,
                        l.Longitude
                    })
                    .FirstOrDefault(),

                Destination = _dbContext.Locations
                    .Where(l => l.Id == x.DestinationLocationId)
                    .Select(l => new
                    {
                        l.Id,
                        l.Address,
                        l.City,
                        l.PostalCode,
                        l.Country,
                        l.Latitude,
                        l.Longitude
                    })
                    .FirstOrDefault(),

                Vehicle = _dbContext.Vehicles
                    .Where(v => v.Id == x.VehicleId)
                    .Select(v => new
                    {
                        v.Id,
                        v.Type,
                        v.Brand,
                        v.Model,
                        v.LicensePlate,
                        v.MaxWeightKg,
                        v.MaxVolumeM3,
                        v.HasTailLift,
                        v.IsRefrigerated
                    })
                    .FirstOrDefault(),

                Cargos = _dbContext.TripCargos
                    .Where(tc => tc.TripId == x.Id)
                    .Select(tc => new
                    {
                        tc.Id,
                        tc.TransportRequestId,
                        tc.WeightKg,
                        tc.VolumeM3,
                        tc.Status,
                        tc.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return trip;
    }

    public async Task<object> Handle(
        Guid? transporterProfileId = null,
        Guid? vehicleId = null,
        TripStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

        var query = _dbContext.Trips.AsQueryable();

        if (transporterProfileId.HasValue)
        {
            query = query.Where(x =>
                x.TransporterProfileId == transporterProfileId.Value);
        }

        if (vehicleId.HasValue)
        {
            query = query.Where(x => x.VehicleId == vehicleId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.DepartureDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.TransporterProfileId,
                x.VehicleId,
                x.DepartureDate,
                x.EstimatedArrivalDate,
                x.AvailableWeightKg,
                x.AvailableVolumeM3,
                x.Status,

                Origin = _dbContext.Locations
                    .Where(l => l.Id == x.OriginLocationId)
                    .Select(l => new
                    {
                        l.Id,
                        l.City
                    })
                    .FirstOrDefault(),

                Destination = _dbContext.Locations
                    .Where(l => l.Id == x.DestinationLocationId)
                    .Select(l => new
                    {
                        l.Id,
                        l.City
                    })
                    .FirstOrDefault(),

                Vehicle = _dbContext.Vehicles
                    .Where(v => v.Id == x.VehicleId)
                    .Select(v => new
                    {
                        v.Id,
                        v.Type,
                        v.Brand,
                        v.Model,
                        v.LicensePlate
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)pageSize);

        return new
        {
            items,
            page,
            pageSize,
            totalItems,
            totalPages
        };
    }
}