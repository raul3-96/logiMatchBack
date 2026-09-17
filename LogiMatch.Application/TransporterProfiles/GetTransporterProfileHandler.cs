using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.TransporterProfiles;

public class GetTransporterProfileHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetTransporterProfileHandler(
        IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<object?> Handle(Guid id)
    {
        var profile = await _dbContext.TransporterProfiles
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.CompanyId,
                x.CreatedAt,

                User = _dbContext.Users
                    .Where(u => u.Id == x.UserId)
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        u.Phone,
                        u.Status
                    })
                    .FirstOrDefault(),

                Company = x.CompanyId == null
                    ? null
                    : _dbContext.Companies
                        .Where(c => c.Id == x.CompanyId)
                        .Select(c => new
                        {
                            c.Id,
                            c.Name,
                            c.TaxId,
                            c.Email,
                            c.Phone
                        })
                        .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        return profile;
    }

    public async Task<object> HandleAll(
        Guid? userId = null,
        Guid? companyId = null)
    {
        var query = _dbContext.TransporterProfiles.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

        if (companyId.HasValue)
        {
            query = query.Where(x => x.CompanyId == companyId.Value);
        }

        var profiles = await query
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.CompanyId,
                x.CreatedAt,

                User = _dbContext.Users
                    .Where(u => u.Id == x.UserId)
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        u.Phone
                    })
                    .FirstOrDefault(),

                Company = x.CompanyId == null
                    ? null
                    : _dbContext.Companies
                        .Where(c => c.Id == x.CompanyId)
                        .Select(c => new
                        {
                            c.Id,
                            c.Name,
                            c.TaxId
                        })
                        .FirstOrDefault()
            })
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return profiles;
    }
}