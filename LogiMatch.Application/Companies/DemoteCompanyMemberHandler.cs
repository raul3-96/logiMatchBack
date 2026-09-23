using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Companies;

public class DemoteCompanyMemberHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public DemoteCompanyMemberHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(Guid companyId, Guid memberId)
    {
        var currentUserId = _currentUserService.UserId;

        var company = await _dbContext.Companies
            .SingleOrDefaultAsync(x => x.Id == companyId);

        if (company == null)
            throw new InvalidOperationException(
                "The specified company does not exist.");

        // Solo el propietario puede gestionar los roles.
        if (company.OwnerUserId != currentUserId)
            throw new InvalidOperationException(
                "Only the company owner can change member roles.");

        var member = await _dbContext.CompanyMembers
            .SingleOrDefaultAsync(x =>
                x.Id == memberId &&
                x.CompanyId == companyId);

        if (member == null)
            throw new InvalidOperationException(
                "The specified company member does not exist.");

        if (!member.IsActive)
            throw new InvalidOperationException(
                "The company member is inactive.");

        if (member.UserId == company.OwnerUserId)
            throw new InvalidOperationException(
                "The company owner cannot be demoted.");

        if (member.Role == CompanyMemberRole.Worker)
            throw new InvalidOperationException(
                "The company member is already a worker.");

        member.DemoteToWorker();

        await _dbContext.SaveChangesAsync();
    }
}