using LogiMatch.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Companies;

public class ActivateCompanyMemberHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public ActivateCompanyMemberHandler(
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

        var member = await _dbContext.CompanyMembers
            .SingleOrDefaultAsync(x =>
                x.Id == memberId &&
                x.CompanyId == companyId);

        if (member == null)
            throw new InvalidOperationException(
                "The specified company member does not exist.");

        var isOwner = company.OwnerUserId == currentUserId;

        var isAdmin = await _dbContext.CompanyMembers
            .AnyAsync(x =>
                x.CompanyId == companyId &&
                x.UserId == currentUserId &&
                x.IsActive &&
                x.Role == Domain.Enums.CompanyMemberRole.Admin);

        if (!isOwner && !isAdmin)
            throw new InvalidOperationException(
                "Only the company owner or an administrator can activate members.");

        if (member.IsActive)
            throw new InvalidOperationException(
                "The company member is already active.");

        member.Activate();

        await _dbContext.SaveChangesAsync();
    }
}