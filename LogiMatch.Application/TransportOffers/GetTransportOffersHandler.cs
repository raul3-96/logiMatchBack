using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.TransportOffers;

public class GetTransportOffersHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetTransportOffersHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<object?> Handle(Guid id)
    {
        var currentUserId = _currentUserService.UserId;
        var offer = await _dbContext.TransportOffers
            .Where(x =>
                x.Id == id &&
                (
                    _dbContext.TransporterProfiles
                        .Any(tp =>
                            tp.Id == x.TransporterProfileId &&
                            tp.UserId == currentUserId)
                    ||
                    _dbContext.TransportRequests
                        .Any(tr =>
                            tr.Id == x.TransportRequestId &&
                            tr.CustomerId == currentUserId)
                ))
            .Select(x => new
            {
                x.Id,
                x.TransportRequestId,
                x.TransporterProfileId,
                x.VehicleId,
                x.Price,
                x.EstimatedPickupDate,
                x.EstimatedDeliveryDate,
                x.Status,
                x.CreatedAt,

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

                Transporter = _dbContext.TransporterProfiles
                    .Where(tp => tp.Id == x.TransporterProfileId)
                    .Select(tp => new
                    {
                        tp.Id,

                        User = _dbContext.Users
                            .Where(u => u.Id == tp.UserId)
                            .Select(u => new
                            {
                                u.Id,
                                u.FirstName,
                                u.LastName,
                                u.Email,
                                u.Phone
                            })
                            .FirstOrDefault(),

                        Company = tp.CompanyId == null
                            ? null
                            : _dbContext.Companies
                                .Where(c => c.Id == tp.CompanyId)
                                .Select(c => new
                                {
                                    c.Id,
                                    c.Name,
                                    c.TaxId
                                })
                                .FirstOrDefault()
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        return offer;
    }

    public async Task<object> HandleAll(
        Guid? transportRequestId = null,
        Guid? transporterProfileId = null,
        Guid? vehicleId = null,
        TransportOfferStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

        var currentUserId = _currentUserService.UserId;

        var query = _dbContext.TransportOffers
            .Where(x =>
                _dbContext.TransporterProfiles
                    .Any(tp =>
                        tp.Id == x.TransporterProfileId &&
                        tp.UserId == currentUserId)
                ||
                _dbContext.TransportRequests
                    .Any(tr =>
                        tr.Id == x.TransportRequestId &&
                        tr.CustomerId == currentUserId));

        if (transportRequestId.HasValue)
        {
            query = query.Where(x =>
                x.TransportRequestId == transportRequestId.Value);
        }

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
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.TransportRequestId,
                x.TransporterProfileId,
                x.VehicleId,
                x.Price,
                x.EstimatedPickupDate,
                x.EstimatedDeliveryDate,
                x.Status,
                x.CreatedAt,

                Vehicle = _dbContext.Vehicles
                    .Where(v => v.Id == x.VehicleId)
                    .Select(v => new
                    {
                        v.Id,
                        v.Type,
                        v.Brand,
                        v.Model,
                        v.LicensePlate,
                        v.HasTailLift,
                        v.IsRefrigerated
                    })
                    .FirstOrDefault(),

                Transporter = _dbContext.TransporterProfiles
                    .Where(tp => tp.Id == x.TransporterProfileId)
                    .Select(tp => new
                    {
                        tp.Id,

                        User = _dbContext.Users
                            .Where(u => u.Id == tp.UserId)
                            .Select(u => new
                            {
                                u.Id,
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
                                    c.Id,
                                    c.Name
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

    public async Task<object> HandleByRequest(
        Guid transportRequestId,
        int page = 1,
        int pageSize = 20)
    {
        var offers = await HandleAll(
            transportRequestId: transportRequestId,
            page: page,
            pageSize: pageSize);

        return offers;
    }
}