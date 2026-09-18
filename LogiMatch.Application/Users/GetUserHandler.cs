using LogiMatch.Domain;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Users;

public class GetUserHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetUserHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<object?> Handle(Guid id)
    {
        var user = await _dbContext.Users
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.FirstName,
                x.LastName,
                x.Phone,
                x.CreatedAt,
                x.Status
            })
            .FirstOrDefaultAsync();

        return user;
    }

    public async Task<object> HandleAll(UserStatus? status = null)
    {
        var query = _dbContext.Users.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var users = await query
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.FirstName,
                x.LastName,
                x.Phone,
                x.CreatedAt,
                x.Status
            })
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToListAsync();

        return users;
    }
}