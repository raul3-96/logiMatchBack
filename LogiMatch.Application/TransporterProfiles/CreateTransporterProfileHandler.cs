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
                "The user does not exist.");

        var profileExists = await _dbContext.TransporterProfiles
            .AnyAsync(x => x.UserId == userId);

        if (profileExists)
            throw new InvalidOperationException(
                "The user already has a transporter profile.");

        var profile = new TransporterProfile(
            userId,
            command.CompanyId);

        _dbContext.TransporterProfiles.Add(profile);

        await _dbContext.SaveChangesAsync();

        return profile.Id;
    }
}