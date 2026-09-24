using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Common.Services;

public class TripManagementAccessService : ITripManagementAccessService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public TripManagementAccessService(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<Guid>> GetManageableTransporterProfileIdsAsync()
    {
        var currentUserId = _currentUserService.UserId;

        var ownProfiles = _dbContext.TransporterProfiles
            .Where(x => x.UserId == currentUserId)
            .Select(x => x.Id);

        var ownedCompanyIds = _dbContext.Companies
            .Where(x => x.OwnerUserId == currentUserId)
            .Select(x => x.Id);

        var adminCompanyIds = _dbContext.CompanyMembers
            .Where(x =>
                x.UserId == currentUserId &&
                x.IsActive &&
                x.Role == CompanyMemberRole.Admin)
            .Select(x => x.CompanyId);

        var manageableCompanyIds = ownedCompanyIds
            .Union(adminCompanyIds);

        var companyProfiles = _dbContext.TransporterProfiles
            .Where(x =>
                x.CompanyId.HasValue &&
                manageableCompanyIds.Contains(x.CompanyId.Value))
            .Select(x => x.Id);

        return await ownProfiles
            .Union(companyProfiles)
            .ToListAsync();
    }
}