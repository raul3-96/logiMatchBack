using LogiMatch.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Users;

public class GetUserHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetUserHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<object?> HandleMe()
    {
        var user = await _dbContext.Users
            .Where(x => x.Id == _currentUserService.UserId)
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
}