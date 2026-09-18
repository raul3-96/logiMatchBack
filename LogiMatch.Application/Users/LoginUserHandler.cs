using LogiMatch.Application.Authentication;
using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Users;

public class LoginUserHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginUserHandler(
        IApplicationDbContext dbContext,
        PasswordHasher<User> passwordHasher,
        ITokenService tokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginUserResult> Handle(
        LoginUserCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
            throw new ValidationException(
                "Email cannot be empty.");

        if (string.IsNullOrWhiteSpace(command.Password))
            throw new ValidationException(
                "Password cannot be empty.");

        var email = command.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user == null)
            throw new UnauthorizedException(
                "Invalid email or password.");

        if (user.Status != UserStatus.Active)
            throw new InvalidOperationException(
                "User is not active.");

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            throw new UnauthorizedException(
                "Invalid email or password.");

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            command.Password);

        if (verification == PasswordVerificationResult.Failed)
            throw new InvalidOperationException(
                "Invalid email or password.");

        var accessToken = _tokenService.GenerateToken(user);

        return new LoginUserResult(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            accessToken);
    }
}