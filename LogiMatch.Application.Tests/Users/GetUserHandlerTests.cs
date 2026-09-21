using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.Users;
using LogiMatch.Domain.Entities;
using Xunit;

namespace LogiMatch.Application.Tests.Users;

public class GetUserHandlerTests
{
    [Fact]
    public async Task HandleMe_WhenCurrentUserDoesNotExist_ShouldReturnNull()
    {
        using var db = TestDbContextFactory.Create();

        var handler = new GetUserHandler(
            db,
            new MockCurrentUserService(Guid.NewGuid()));

        var result = await handler.HandleMe();

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleMe_WhenCurrentUserExists_ShouldReturnCurrentUser()
    {
        using var db = TestDbContextFactory.Create();

        var user = new User(
            "user@example.com",
            "John",
            "Doe",
            "600123456");

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new GetUserHandler(
            db,
            new MockCurrentUserService(user.Id));

        var result = await handler.HandleMe();

        Assert.NotNull(result);

        var resultType = result!.GetType();

        Assert.Equal(
            user.Id,
            resultType.GetProperty("Id")!.GetValue(result));

        Assert.Equal(
            user.Email,
            resultType.GetProperty("Email")!.GetValue(result));

        Assert.Equal(
            user.FirstName,
            resultType.GetProperty("FirstName")!.GetValue(result));

        Assert.Equal(
            user.LastName,
            resultType.GetProperty("LastName")!.GetValue(result));

        Assert.Equal(
            user.Phone,
            resultType.GetProperty("Phone")!.GetValue(result));

        Assert.Equal(
            user.Status,
            resultType.GetProperty("Status")!.GetValue(result));
    }

    [Fact]
    public async Task HandleMe_WhenThereAreMultipleUsers_ShouldReturnOnlyAuthenticatedUser()
    {
        using var db = TestDbContextFactory.Create();

        var currentUser = new User(
            "current@example.com",
            "Current",
            "User",
            "600123456");

        var otherUser = new User(
            "other@example.com",
            "Other",
            "User",
            "600999888");

        db.Users.Add(currentUser);
        db.Users.Add(otherUser);
        await db.SaveChangesAsync();

        var handler = new GetUserHandler(
            db,
            new MockCurrentUserService(currentUser.Id));

        var result = await handler.HandleMe();

        Assert.NotNull(result);

        var resultType = result!.GetType();

        Assert.Equal(
            currentUser.Id,
            resultType.GetProperty("Id")!.GetValue(result));

        Assert.Equal(
            currentUser.Email,
            resultType.GetProperty("Email")!.GetValue(result));

        Assert.NotEqual(
            otherUser.Id,
            resultType.GetProperty("Id")!.GetValue(result));
    }
}