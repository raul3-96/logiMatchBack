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

        // Perfiles propios:
        // - Perfil personal sin empresa -> permitido.
        // - Perfil de una empresa -> solo si el usuario es Owner
        //   o miembro activo de esa empresa.
        var ownProfiles = _dbContext.TransporterProfiles
            .Where(x =>
                x.UserId == currentUserId &&
                (
                    !x.CompanyId.HasValue ||

                    _dbContext.Companies.Any(c =>
                        c.Id == x.CompanyId.Value &&
                        c.OwnerUserId == currentUserId) ||

                    _dbContext.CompanyMembers.Any(m =>
                        m.CompanyId == x.CompanyId.Value &&
                        m.UserId == currentUserId &&
                        m.IsActive)
                ))
            .Select(x => x.Id);

        // Empresas donde el usuario es Owner.
        var ownedCompanyIds = _dbContext.Companies
            .Where(x => x.OwnerUserId == currentUserId)
            .Select(x => x.Id);

        // Empresas donde el usuario es Admin activo.
        var adminCompanyIds = _dbContext.CompanyMembers
            .Where(x =>
                x.UserId == currentUserId &&
                x.IsActive &&
                x.Role == CompanyMemberRole.Admin)
            .Select(x => x.CompanyId);

        var manageableCompanyIds = ownedCompanyIds
            .Union(adminCompanyIds);

        // Todos los perfiles pertenecientes a empresas que el usuario
        // puede administrar.
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