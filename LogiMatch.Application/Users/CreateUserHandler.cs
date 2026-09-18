using LogiMatch.Domain.Entities;

namespace LogiMatch.Application.Users;

public class CreateUserHandler
{
    private readonly IApplicationDbContext _dbContext;

    public CreateUserHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(
        CreateUserCommand command)
    {
        var user = new User(
            command.Email,
            command.FirstName,
            command.LastName,
            command.Phone);

        _dbContext.Users.Add(user);

        await _dbContext.SaveChangesAsync();

        return user.Id;
    }
}