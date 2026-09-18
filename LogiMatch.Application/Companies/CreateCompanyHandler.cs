using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Companies;

public class CreateCompanyHandler
{
    private readonly IApplicationDbContext _dbContext;

    public CreateCompanyHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateCompanyCommand command)
    {
        var taxIdExists = await _dbContext.Companies
            .AnyAsync(x => x.TaxId == command.TaxId.Trim().ToUpperInvariant());

        if (taxIdExists)
            throw new InvalidOperationException(
                "A company with the specified Tax ID already exists.");

        var company = new Company(
            command.Name,
            command.TaxId,
            command.Email,
            command.Phone);

        _dbContext.Companies.Add(company);

        await _dbContext.SaveChangesAsync();

        return company.Id;
    }
}