using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Cargos;

public class GetCargoHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetCargoHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<object?> Handle(Guid id)
    {
        var cargo = await _dbContext.Cargos
            .Where(x => x.Id == id)
            .Join(
                _dbContext.TransportRequests,
                c => c.TransportRequestId,
                tr => tr.Id,
                (c, tr) => new { Cargo = c, TransportRequest = tr })
            .Where(x => x.TransportRequest.CustomerId == _currentUserService.UserId)
            .Select(x => new
            {
                x.Cargo.Id,
                x.Cargo.TransportRequestId,
                x.Cargo.Description,
                x.Cargo.WeightKg,
                x.Cargo.VolumeM3,
                x.Cargo.Quantity,
                x.Cargo.RequiresRefrigeration,
                x.Cargo.RequiresTailLift
            })
            .FirstOrDefaultAsync();

        return cargo;
    }

    public async Task<object> HandleAll(
        Guid? transportRequestId = null)
    {
        var query = _dbContext.Cargos
            .Join(
                _dbContext.TransportRequests,
                c => c.TransportRequestId,
                tr => tr.Id,
                (c, tr) => new { Cargo = c, TransportRequest = tr })
            .Where(x => x.TransportRequest.CustomerId == _currentUserService.UserId);

        if (transportRequestId.HasValue)
        {
            query = query.Where(x =>
                x.Cargo.TransportRequestId == transportRequestId.Value);
        }

        var cargos = await query
            .Select(x => new
            {
                x.Cargo.Id,
                x.Cargo.TransportRequestId,
                x.Cargo.Description,
                x.Cargo.WeightKg,
                x.Cargo.VolumeM3,
                x.Cargo.Quantity,
                x.Cargo.RequiresRefrigeration,
                x.Cargo.RequiresTailLift
            })
            .OrderBy(x => x.Description)
            .ToListAsync();

        return cargos;
    }
}