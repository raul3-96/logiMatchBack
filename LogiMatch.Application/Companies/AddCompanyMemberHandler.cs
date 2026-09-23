using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Companies;

public class AddCompanyMemberHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public AddCompanyMemberHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        Guid companyId,
        AddCompanyMemberCommand command)
    {
        var currentUserId = _currentUserService.UserId;

        var company = await _dbContext.Companies
            .SingleOrDefaultAsync(x => x.Id == companyId);

        if (company == null)
            throw new InvalidOperationException(
                "The specified company does not exist.");

        // Solo el propietario puede añadir miembros.
        if (company.OwnerUserId != currentUserId)
            throw new InvalidOperationException(
                "Only the company owner can add members.");

        // El propietario no puede añadirse como miembro.
        if (command.UserId == company.OwnerUserId)
            throw new InvalidOperationException(
                "The company owner cannot be added as a member.");

        // El usuario autenticado no puede añadirse a sí mismo.
        if (command.UserId == currentUserId)
            throw new InvalidOperationException(
                "The authenticated user cannot add themselves as a company member.");

        var userExists = await _dbContext.Users
            .AnyAsync(x => x.Id == command.UserId);

        if (!userExists)
            throw new InvalidOperationException(
                "The specified user does not exist.");

        var alreadyMember = await _dbContext.CompanyMembers
            .AnyAsync(x =>
                x.CompanyId == companyId &&
                x.UserId == command.UserId);

        if (alreadyMember)
            throw new InvalidOperationException(
                "The user is already a member of this company.");

        // Los nuevos miembros siempre empiezan como Worker.
        var member = new CompanyMember(
            companyId,
            command.UserId);

        _dbContext.CompanyMembers.Add(member);

        await _dbContext.SaveChangesAsync();

        return member.Id;
    }
}