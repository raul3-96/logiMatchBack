using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Matching;

public class GetTripCargoHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITripManagementAccessService _tripManagementAccessService;

    public GetTripCargoHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ITripManagementAccessService tripManagementAccessService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _tripManagementAccessService = tripManagementAccessService;
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

        if (tripCargo == null)
            return null;

        var trip = await _dbContext.Trips
            .FirstOrDefaultAsync(t => t.Id == tripCargo.TripId);

        if (trip == null)
            throw new NotFoundException(
                "Trip not found.");

        var allowedProfileIds =
            await _tripManagementAccessService
                .GetManageableTransporterProfileIdsAsync();

        var isTransporter =
            allowedProfileIds.Contains(trip.TransporterProfileId);

        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(r => r.Id == tripCargo.TransportRequestId);

        if (request == null)
            throw new NotFoundException(
                "Transport request not found.");

        var isCustomer =
            request.CustomerId == _currentUserService.UserId;

        if (!isTransporter && !isCustomer)
            throw new ConflictException(
                "You do not have permission to view this trip cargo.");

        return tripCargo;
    }

    public async Task<object> HandleAll(
        Guid? tripId = null,
        Guid? transportRequestId = null,
        TripCargoStatus? status = null)
    {
        var allowedProfileIds =
            await _tripManagementAccessService
                .GetManageableTransporterProfileIdsAsync();

        var userTripIds = await _dbContext.Trips
            .Where(t => allowedProfileIds.Contains(t.TransporterProfileId))
            .Select(t => t.Id)
            .ToListAsync();

        var userRequestIds = await _dbContext.TransportRequests
            .Where(r => r.CustomerId == _currentUserService.UserId)
            .Select(r => r.Id)
            .ToListAsync();

        var query = _dbContext.TripCargos
            .Where(x =>
                userTripIds.Contains(x.TripId) ||
                userRequestIds.Contains(x.TransportRequestId));

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
                    .FirstOrDefault(),

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
                    .FirstOrDefault()
            })
            .ToListAsync();

        return tripCargos;
    }
}