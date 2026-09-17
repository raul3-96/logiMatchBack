using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Locations;

public class GetLocationHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetLocationHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<object?> Handle(Guid id)
    {
        var location = await _dbContext.Locations
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Address,
                x.City,
                x.PostalCode,
                x.Country,
                x.Latitude,
                x.Longitude
            })
            .FirstOrDefaultAsync();

        return location;
    }
}