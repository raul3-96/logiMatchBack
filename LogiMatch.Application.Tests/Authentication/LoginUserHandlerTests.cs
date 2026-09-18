using LogiMatch.Application.Authentication;
using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.Users;
using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMatch.Application.Tests.Authentication;

public class LoginUserHandlerTests
{
    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ShouldReturnToken()
    {
        using var db = TestDbContextFactory.Create();
        var user = CreateUser("user@example.com", "MySecurePassword123!");

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new LoginUserHandler(
            db,
            new PasswordHasher<User>(),
            new FakeTokenService());

        var result = await handler.Handle(
            new LoginUserCommand(
                "  USER@EXAMPLE.COM  ",
                "MySecurePassword123!"));

        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("user@example.com", result.Email);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("test-token", result.AccessToken);
    }

    [Fact]
    public async Task Handle_WhenPasswordIsInvalid_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();
        var user = CreateUser("user@example.com", "MySecurePassword123!");

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new LoginUserHandler(
            db,
            new PasswordHasher<User>(),
            new FakeTokenService());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new LoginUserCommand(
                    "user@example.com",
                    "WrongPassword123!")));

        Assert.Equal(
            "Invalid email or password.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();
        var user = CreateUser("user@example.com", "MySecurePassword123!");

        var inactiveStatus = Enum.GetValues<UserStatus>()
            .First(x => x != UserStatus.Active);

        typeof(User)
            .GetProperty(nameof(User.Status))!
            .SetValue(user, inactiveStatus);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new LoginUserHandler(
            db,
            new PasswordHasher<User>(),
            new FakeTokenService());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new LoginUserCommand(
                    "user@example.com",
                    "MySecurePassword123!")));

        Assert.Equal(
            "User is not active.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenEmailIsEmpty_ShouldThrow(string email)
    {
        using var db = TestDbContextFactory.Create();
        var handler = new LoginUserHandler(
            db,
            new PasswordHasher<User>(),
            new FakeTokenService());

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(
                new LoginUserCommand(
                    email,
                    "MySecurePassword123!")));

        Assert.Equal(
            "Email cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenPasswordIsEmpty_ShouldThrow(string password)
    {
        using var db = TestDbContextFactory.Create();
        var handler = new LoginUserHandler(
            db,
            new PasswordHasher<User>(),
            new FakeTokenService());

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(
                new LoginUserCommand(
                    "user@example.com",
                    password)));

        Assert.Equal(
            "Password cannot be empty.",
            exception.Message);
    }

    private static User CreateUser(
        string email,
        string password)
    {
        var user = new User(
            email,
            "John",
            "Doe",
            "600123456");

        var passwordHasher = new PasswordHasher<User>();
        var passwordHash = passwordHasher.HashPassword(user, password);

        user.SetPasswordHash(passwordHash);

        return user;
    }

    private sealed class FakeTokenService : ITokenService
    {
        public string GenerateToken(User user)
        {
            return "test-token";
        }
    }
}