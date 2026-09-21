using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Companies;

public class CreateCompanyHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateCompanyHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateCompanyCommand command)
    {
        var ownerUserId = _currentUserService.UserId;

        var userExists = await _dbContext.Users
            .AnyAsync(x => x.Id == ownerUserId);

        if (!userExists)
            throw new InvalidOperationException(
                "The authenticated user does not exist.");

        var taxIdExists = await _dbContext.Companies
            .AnyAsync(x => x.TaxId == command.TaxId.Trim().ToUpperInvariant());

        if (taxIdExists)
            throw new InvalidOperationException(
                "A company with the specified Tax ID already exists.");

        var company = new Company(
            command.Name,
            command.TaxId,
            command.Email,
            command.Phone,
            ownerUserId);

        _dbContext.Companies.Add(company);

        await _dbContext.SaveChangesAsync();

        return company.Id;
    }
}
