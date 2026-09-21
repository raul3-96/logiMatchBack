using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.TransporterProfiles;

public class CreateTransporterProfileHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateTransporterProfileHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        CreateTransporterProfileCommand command)
    {
        var userId = _currentUserService.UserId;

        var userExists = await _dbContext.Users
            .AnyAsync(x => x.Id == userId);

        if (!userExists)
            throw new InvalidOperationException(
                "The specified user does not exist.");

        var profileExists = await _dbContext.TransporterProfiles
            .AnyAsync(x => x.UserId == userId);

        if (profileExists)
            throw new InvalidOperationException(
                "The user already has a transporter profile.");

        if (command.CompanyId.HasValue)
        {
            var company = await _dbContext.Companies
                .SingleOrDefaultAsync(x => x.Id == command.CompanyId.Value);

            if (company == null)
                throw new InvalidOperationException(
                    "The specified company does not exist.");

            if (company.OwnerUserId != userId)
                throw new InvalidOperationException(
                    "The user does not own the specified company.");
        }

        var profile = new TransporterProfile(
            userId,
            command.CompanyId);

        _dbContext.TransporterProfiles.Add(profile);

        await _dbContext.SaveChangesAsync();

        return profile.Id;
    }
}
