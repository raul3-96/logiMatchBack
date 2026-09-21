using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Bookings;

public class GetBookingHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetBookingHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<object?> Handle(Guid id)
    {
        var currentUserId = _currentUserService.UserId;

        var booking = await _dbContext.Bookings
            .Where(x =>
                x.Id == id &&
                (
                    _dbContext.TransportRequests.Any(r =>
                        r.Id == x.TransportRequestId &&
                        r.CustomerId == currentUserId)
                    ||
                    _dbContext.TransportOffers.Any(o =>
                        o.Id == x.TransportOfferId &&
                        _dbContext.TransporterProfiles.Any(tp =>
                            tp.Id == o.TransporterProfileId &&
                            tp.UserId == currentUserId))
                ))
            .Select(x => new
            {
                x.Id,
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

                        PickupLocation = _dbContext.Locations
                            .Where(l => l.Id == r.PickupLocationId)
                            .Select(l => new
                            {
                                l.Id,
                                l.Address,
                                l.City,
                                l.PostalCode,
                                l.Country
                            })
                            .FirstOrDefault(),

                        DeliveryLocation = _dbContext.Locations
                            .Where(l => l.Id == r.DeliveryLocationId)
                            .Select(l => new
                            {
                                l.Id,
                                l.Address,
                                l.City,
                                l.PostalCode,
                                l.Country
                            })
                            .FirstOrDefault()
                    })
                    .FirstOrDefault(),

                Offer = _dbContext.TransportOffers
                    .Where(o => o.Id == x.TransportOfferId)
                    .Select(o => new
                    {
                        o.Id,
                        o.TransporterProfileId,
                        o.VehicleId,
                        o.Price,
                        o.EstimatedPickupDate,
                        o.EstimatedDeliveryDate,
                        o.Status
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        return booking;
    }

    public async Task<object> HandleAll(
        Guid? transportRequestId = null,
        Guid? transporterProfileId = null,
        BookingStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

        var currentUserId = _currentUserService.UserId;

        var query = _dbContext.Bookings
            .Where(x =>
                _dbContext.TransportRequests.Any(r =>
                    r.Id == x.TransportRequestId &&
                    r.CustomerId == currentUserId)
                ||
                _dbContext.TransportOffers.Any(o =>
                    o.Id == x.TransportOfferId &&
                    _dbContext.TransporterProfiles.Any(tp =>
                        tp.Id == o.TransporterProfileId &&
                        tp.UserId == currentUserId)));

        if (transportRequestId.HasValue)
        {
            query = query.Where(x =>
                x.TransportRequestId == transportRequestId.Value);
        }

        if (transporterProfileId.HasValue)
        {
            query = query.Where(x =>
                _dbContext.TransportOffers.Any(o =>
                    o.Id == x.TransportOfferId &&
                    o.TransporterProfileId == transporterProfileId.Value));
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

                        PickupLocation = _dbContext.Locations
                            .Where(l => l.Id == r.PickupLocationId)
                            .Select(l => new
                            {
                                l.Id,
                                l.City
                            })
                            .FirstOrDefault(),

                        DeliveryLocation = _dbContext.Locations
                            .Where(l => l.Id == r.DeliveryLocationId)
                            .Select(l => new
                            {
                                l.Id,
                                l.City
                            })
                            .FirstOrDefault()
                    })
                    .FirstOrDefault(),

                Offer = _dbContext.TransportOffers
                    .Where(o => o.Id == x.TransportOfferId)
                    .Select(o => new
                    {
                        o.Id,
                        o.TransporterProfileId,
                        o.VehicleId,
                        o.Price,
                        o.EstimatedPickupDate,
                        o.EstimatedDeliveryDate,
                        o.Status,

                        Vehicle = _dbContext.Vehicles
                            .Where(v => v.Id == o.VehicleId)
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