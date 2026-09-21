using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LogiMatch.Application.Users;

public class CreateUserHandler
{
    private const int MinimumPasswordLength = 8;
    private const string EmailUniqueConstraint = "UX_users_email";

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

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsEmailUniqueConstraintViolation(ex))
        {
            // The pre-check is only an optimization. The database constraint
            // is the source of truth when requests race during registration.
            throw new ConflictException(
                "A user with the specified email already exists.");
        }

        return user.Id;
    }

    private static bool IsEmailUniqueConstraintViolation(DbUpdateException exception)
    {
        for (var current = exception.InnerException;
             current is not null;
             current = current.InnerException)
        {
            if (current.Message.Contains(
                    EmailUniqueConstraint,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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
