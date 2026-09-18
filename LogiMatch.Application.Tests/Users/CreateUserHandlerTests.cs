using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMatch.Application.Tests.Users;

public class CreateUserHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserIsValid_ShouldCreateUser()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateUserHandler(db);

        var command = new CreateUserCommand
        {
            Email = "  USER@EXAMPLE.COM  ",
            FirstName = "  John  ",
            LastName = "  Doe  ",
            Phone = "  600123456  "
        };

        var userId = await handler.Handle(command);

        var user = await db.Users
            .SingleAsync(x => x.Id == userId);

        Assert.NotEqual(Guid.Empty, userId);
        Assert.Equal("user@example.com", user.Email);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("600123456", user.Phone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenEmailIsEmpty_ShouldThrow(string email)
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateUserHandler(db);

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
        var handler = new CreateUserHandler(db);

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
        var handler = new CreateUserHandler(db);

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
        var handler = new CreateUserHandler(db);

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
        var handler = new CreateUserHandler(db);

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
        string phone = "600123456")
    {
        return new CreateUserCommand
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone
        };
    }
}