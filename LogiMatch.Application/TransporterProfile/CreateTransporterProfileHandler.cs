using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.TransporterProfiles;

public class CreateTransporterProfileHandler
{
    private readonly IApplicationDbContext _dbContext;

    public CreateTransporterProfileHandler(
        IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(
        CreateTransporterProfileCommand command)
    {
        var userExists = await _dbContext.Users
            .AnyAsync(x => x.Id == command.UserId);

        if (!userExists)
            throw new InvalidOperationException(
                "The specified user does not exist.");

        var profileExists = await _dbContext.TransporterProfiles
            .AnyAsync(x => x.UserId == command.UserId);

        if (profileExists)
            throw new InvalidOperationException(
                "The user already has a transporter profile.");

        var profile = new TransporterProfile(
            command.UserId,
            command.CompanyId);

        _dbContext.TransporterProfiles.Add(profile);

        await _dbContext.SaveChangesAsync();

        return profile.Id;
    }
}