using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Matching;

public class GetTripCargoHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetTripCargoHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<object?> Handle(Guid id)
    {
        var tripCargo = await _dbContext.TripCargos
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.TripId,
                x.TransportRequestId,
                x.WeightKg,
                x.VolumeM3,
                x.CreatedAt,
                x.Status,

                Trip = _dbContext.Trips
                    .Where(t => t.Id == x.TripId)
                    .Select(t => new
                    {
                        t.Id,
                        t.TransporterProfileId,
                        t.VehicleId,
                        t.DepartureDate,
                        t.EstimatedArrivalDate,
                        t.Status
                    })
                    .FirstOrDefault(),

                TransportRequest = _dbContext.TransportRequests
                    .Where(r => r.Id == x.TransportRequestId)
                    .Select(r => new
                    {
                        r.Id,
                        r.CustomerId,
                        r.PickupDate,
                        r.DeliveryDate,
                        r.Status
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        return tripCargo;
    }

    public async Task<object> HandleAll(
        Guid? tripId = null,
        Guid? transportRequestId = null,
        TripCargoStatus? status = null)
    {
        var query = _dbContext.TripCargos.AsQueryable();

        if (tripId.HasValue)
        {
            query = query.Where(x => x.TripId == tripId.Value);
        }

        if (transportRequestId.HasValue)
        {
            query = query.Where(x =>
                x.TransportRequestId == transportRequestId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var tripCargos = await query
            .Select(x => new
            {
                x.Id,
                x.TripId,
                x.TransportRequestId,
                x.WeightKg,
                x.VolumeM3,
                x.CreatedAt,
                x.Status,

                TransportRequest = _dbContext.TransportRequests
                    .Where(r => r.Id == x.TransportRequestId)
                    .Select(r => new
                    {
                        r.Id,
                        r.CustomerId,
                        r.PickupDate,
                        r.DeliveryDate,
                        r.Status
                    })
                    .FirstOrDefault()
            })
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return tripCargos;
    }
}