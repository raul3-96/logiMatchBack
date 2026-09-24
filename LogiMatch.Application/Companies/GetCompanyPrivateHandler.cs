using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Companies;

public class GetCompanyPrivateHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetCompanyPrivateHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<object?> Handle(Guid id)
    {
        var company = await _dbContext.Companies
            .SingleOrDefaultAsync(x => x.Id == id);

        if (company == null)
            throw new NotFoundException(
                "The specified company does not exist.");

        var currentUserId = _currentUserService.UserId;

        var isOwner = company.OwnerUserId == currentUserId;

        var isAdmin = await _dbContext.CompanyMembers
            .AnyAsync(x =>
                x.CompanyId == id &&
                x.UserId == currentUserId &&
                x.IsActive &&
                x.Role == CompanyMemberRole.Admin);

        if (!isOwner && !isAdmin)
            throw new ConflictException(
                "Only the company owner or an administrator can access private company details.");

        return new
        {
            company.Id,
            company.Name,
            company.TaxId,
            company.Email,
            company.Phone,
            company.OwnerUserId,
            company.CreatedAt
        };
    }
}