using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.Users;
using LogiMatch.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMatch.Application.Tests.Users;

public class CreateUserHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserIsValid_ShouldCreateUser()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateUserHandler(db, new PasswordHasher<User>());

        var command = new CreateUserCommand
        {
            Email = "  USER@EXAMPLE.COM  ",
            FirstName = "  John  ",
            LastName = "  Doe  ",
            Phone = "  600123456  ",
            Password = "MySecurePassword123!"
        };

        var userId = await handler.Handle(command);

        var user = await db.Users
            .SingleAsync(x => x.Id == userId);

        Assert.NotEqual(Guid.Empty, userId);
        Assert.Equal("user@example.com", user.Email);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("600123456", user.Phone);
        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));

        var passwordHasher = new PasswordHasher<User>();
        var verification = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash!,
            command.Password);

        Assert.Equal(PasswordVerificationResult.Success, verification);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenPasswordIsEmpty_ShouldThrow(string password)
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateUserHandler(db, new PasswordHasher<User>());

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(CreateCommand(password: password)));

        Assert.Equal(
            "Password cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    public async Task Handle_WhenPasswordIsTooShort_ShouldThrow(string password)
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateUserHandler(db, new PasswordHasher<User>());

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(CreateCommand(password: password)));

        Assert.Equal(
            "Password must be at least 8 characters long.",
            exception.Message);
    }

    [Theory]
    [InlineData("lowercase123!", "Password must contain at least one uppercase letter.")]
    [InlineData("UPPERCASE123!", "Password must contain at least one lowercase letter.")]
    [InlineData("PasswordOnly!", "Password must contain at least one number.")]
    [InlineData("Password123", "Password must contain at least one symbol.")]
    public async Task Handle_WhenPasswordDoesNotMeetComplexityRules_ShouldThrow(
        string password,
        string expectedMessage)
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateUserHandler(db, new PasswordHasher<User>());

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(CreateCommand(password: password)));

        Assert.Equal(
            expectedMessage,
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenEmailIsEmpty_ShouldThrow(string email)
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateUserHandler(db, new PasswordHasher<User>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(email: email)));

        Assert.Equal(
            "Email cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    public async Task Handle_WhenEmailFormatIsInvalid_ShouldThrow(string email)
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateUserHandler(db, new PasswordHasher<User>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(email: email)));

        Assert.Equal(
            "Email format is invalid.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenFirstNameIsEmpty_ShouldThrow(string firstName)
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateUserHandler(db, new PasswordHasher<User>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(firstName: firstName)));

        Assert.Equal(
            "First name cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenLastNameIsEmpty_ShouldThrow(string lastName)
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateUserHandler(db, new PasswordHasher<User>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(lastName: lastName)));

        Assert.Equal(
            "Last name cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenPhoneIsEmpty_ShouldThrow(string phone)
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateUserHandler(db, new PasswordHasher<User>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(phone: phone)));

        Assert.Equal(
            "Phone cannot be empty.",
            exception.Message);
    }

    private static CreateUserCommand CreateCommand(
        string email = "user@example.com",
        string firstName = "John",
        string lastName = "Doe",
        string phone = "600123456",
        string password = "MySecurePassword123!")
    {
        return new CreateUserCommand
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Password = password
        };
    }
}