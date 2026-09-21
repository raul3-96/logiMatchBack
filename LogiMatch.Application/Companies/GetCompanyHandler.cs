using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Companies;

public class GetCompanyHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetCompanyHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<object?> Handle(Guid id)
    {
        var company = await _dbContext.Companies
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.TaxId,
                x.Email,
                x.Phone,
                x.CreatedAt
            })
            .FirstOrDefaultAsync();

        return company;
    }

    public async Task<object> HandleAll()
    {
        var companies = await _dbContext.Companies
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.TaxId,
                x.Email,
                x.Phone,
                x.CreatedAt
            })
            .OrderBy(x => x.Name)
            .ToListAsync();

        return companies;
    }
}