using LogiMatch.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Companies;

public class GetCompanyMembersHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetCompanyMembersHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<object> Handle(Guid companyId)
    {
        var currentUserId = _currentUserService.UserId;

        var company = await _dbContext.Companies
            .SingleOrDefaultAsync(x => x.Id == companyId);

        if (company == null)
            throw new InvalidOperationException(
                "The specified company does not exist.");

        var isOwner = company.OwnerUserId == currentUserId;

        var isActiveMember = await _dbContext.CompanyMembers
            .AnyAsync(x =>
                x.CompanyId == companyId &&
                x.UserId == currentUserId &&
                x.IsActive);

        if (!isOwner && !isActiveMember)
            throw new InvalidOperationException(
                "The user is not an active member of the specified company.");

        return await _dbContext.CompanyMembers
            .Where(x => x.CompanyId == companyId)
            .Join(
                _dbContext.Users,
                member => member.UserId,
                user => user.Id,
                (member, user) => new
                {
                    member.Id,
                    member.CompanyId,
                    member.UserId,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    member.Role,
                    member.IsActive,
                    member.CreatedAt
                })
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToListAsync();
    }
}