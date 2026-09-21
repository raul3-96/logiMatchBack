using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LogiMatch.Application.Users;

public class CreateUserHandler
{
    private const int MinimumPasswordLength = 8;

    private readonly IApplicationDbContext _dbContext;
    private readonly PasswordHasher<User> _passwordHasher;

    public CreateUserHandler(
        IApplicationDbContext dbContext,
        PasswordHasher<User> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(
        CreateUserCommand command)
    {
        ValidatePassword(command.Password);

        var user = new User(
            command.Email,
            command.FirstName,
            command.LastName,
            command.Phone);

        var email = command.Email.Trim().ToLowerInvariant();

        var exists = await _dbContext.Users
            .AnyAsync(x => x.Email == email);

        if (exists)
            throw new ConflictException(
                "A user with the specified email already exists.");

        var passwordHash = _passwordHasher.HashPassword(
            user,
            command.Password);

        user.SetPasswordHash(passwordHash);

        _dbContext.Users.Add(user);

        await _dbContext.SaveChangesAsync();

        return user.Id;
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ValidationException(
                "Password cannot be empty.");

        if (password.Length < MinimumPasswordLength)
            throw new ValidationException(
                "Password must be at least 8 characters long.");

        if (!password.Any(char.IsUpper))
            throw new ValidationException(
                "Password must contain at least one uppercase letter.");

        if (!password.Any(char.IsLower))
            throw new ValidationException(
                "Password must contain at least one lowercase letter.");

        if (!password.Any(char.IsDigit))
            throw new ValidationException(
                "Password must contain at least one number.");

        if (!password.Any(char.IsPunctuation) &&
            !password.Any(char.IsSymbol))
        {
            throw new ValidationException(
                "Password must contain at least one symbol.");
        }
    }
}