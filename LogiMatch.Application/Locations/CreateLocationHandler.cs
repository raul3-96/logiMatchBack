using LogiMatch.Domain.Entities;

namespace LogiMatch.Application.Locations;

public class CreateLocationHandler
{
    private readonly IApplicationDbContext _dbContext;

    public CreateLocationHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(
        CreateLocationCommand command)
    {
        var location = new Location(
            command.Address,
            command.City,
            command.PostalCode,
            command.Country,
            command.Latitude,
            command.Longitude);

        _dbContext.Locations.Add(location);

        await _dbContext.SaveChangesAsync();

        return location.Id;
    }
}