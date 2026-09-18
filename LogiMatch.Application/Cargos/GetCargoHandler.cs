using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Cargos;

public class GetCargoHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetCargoHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<object?> Handle(Guid id)
    {
        var cargo = await _dbContext.Cargos
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.TransportRequestId,
                x.Description,
                x.WeightKg,
                x.VolumeM3,
                x.Quantity,
                x.RequiresRefrigeration,
                x.RequiresTailLift
            })
            .FirstOrDefaultAsync();

        return cargo;
    }

    public async Task<object> HandleAll(
        Guid? transportRequestId = null)
    {
        var query = _dbContext.Cargos.AsQueryable();

        if (transportRequestId.HasValue)
        {
            query = query.Where(x =>
                x.TransportRequestId == transportRequestId.Value);
        }

        var cargos = await query
            .Select(x => new
            {
                x.Id,
                x.TransportRequestId,
                x.Description,
                x.WeightKg,
                x.VolumeM3,
                x.Quantity,
                x.RequiresRefrigeration,
                x.RequiresTailLift
            })
            .OrderBy(x => x.Description)
            .ToListAsync();

        return cargos;
    }
}