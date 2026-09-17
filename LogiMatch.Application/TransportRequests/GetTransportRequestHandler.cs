using LogiMatch.Application;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.TransportRequests;

public class GetTransportRequestHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetTransportRequestHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<object?> Handle(Guid id)
    {
        var request = await _dbContext.TransportRequests
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.CustomerId,

                PickupLocation = _dbContext.Locations
                    .Where(l => l.Id == x.PickupLocationId)
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

                DeliveryLocation = _dbContext.Locations
                    .Where(l => l.Id == x.DeliveryLocationId)
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

                x.PickupDate,
                x.DeliveryDate,
                x.Status,
                x.CreatedAt,

                Cargos = x.Cargos.Select(c => new
                {
                    c.Id,
                    c.Description,
                    c.WeightKg,
                    c.VolumeM3,
                    c.Quantity,
                    c.RequiresRefrigeration,
                    c.RequiresTailLift
                })
            })
            .FirstOrDefaultAsync();

        return request;
    }

    public async Task<object> Handle(
        Guid? customerId = null,
        TransportRequestStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

        var query = _dbContext.TransportRequests.AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.CustomerId,
                x.PickupDate,
                x.DeliveryDate,
                x.Status,
                x.CreatedAt,

                PickupLocation = _dbContext.Locations
                    .Where(l => l.Id == x.PickupLocationId)
                    .Select(l => new
                    {
                        l.Id,
                        l.City
                    })
                    .FirstOrDefault(),

                DeliveryLocation = _dbContext.Locations
                    .Where(l => l.Id == x.DeliveryLocationId)
                    .Select(l => new
                    {
                        l.Id,
                        l.City
                    })
                    .FirstOrDefault(),

                TotalWeightKg = x.Cargos.Sum(c => c.WeightKg),
                TotalVolumeM3 = x.Cargos.Sum(c => c.VolumeM3)
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