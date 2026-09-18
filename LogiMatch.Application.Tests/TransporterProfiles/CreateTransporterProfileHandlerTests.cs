using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.TransporterProfiles;
using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMatch.Application.Tests.TransporterProfiles;

public class CreateTransporterProfileHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateTransporterProfileHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand()));

        Assert.Equal(
            "The specified user does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyHasTransporterProfile_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var user = CreateUser();
        db.Users.Add(user);

        db.TransporterProfiles.Add(
            new TransporterProfile(user.Id));

        await db.SaveChangesAsync();

        var handler = new CreateTransporterProfileHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(user.Id)));

        Assert.Equal(
            "The user already has a transporter profile.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenUserIsValid_ShouldCreateTransporterProfile()
    {
        using var db = TestDbContextFactory.Create();

        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new CreateTransporterProfileHandler(db);

        var command = CreateCommand(
            user.Id,
            companyId: null);

        var profileId = await handler.Handle(command);

        var profile = await db.TransporterProfiles
            .SingleAsync(x => x.Id == profileId);

        Assert.NotEqual(Guid.Empty, profileId);
        Assert.Equal(user.Id, profile.UserId);
        Assert.Null(profile.CompanyId);
    }

    [Fact]
    public async Task Handle_WhenCompanyIdIsProvided_ShouldCreateTransporterProfile()
    {
        using var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var company = CreateCompany();

        db.Users.Add(user);
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var handler = new CreateTransporterProfileHandler(db);

        var command = CreateCommand(
            user.Id,
            company.Id);

        var profileId = await handler.Handle(command);

        var profile = await db.TransporterProfiles
            .SingleAsync(x => x.Id == profileId);

        Assert.NotEqual(Guid.Empty, profileId);
        Assert.Equal(user.Id, profile.UserId);
        Assert.Equal(company.Id, profile.CompanyId);
    }

    private static CreateTransporterProfileCommand CreateCommand(
        Guid? userId = null,
        Guid? companyId = null)
    {
        return new CreateTransporterProfileCommand
        {
            UserId = userId ?? Guid.NewGuid(),
            CompanyId = companyId
        };
    }

    private static User CreateUser()
    {
        return new User(
            "user@example.com",
            "John",
            "Doe",
            "600123456");
    }

    private static Company CreateCompany()
    {
        return new Company(
            "LogiMatch SL",
            "B12345678",
            "info@logimatch.com",
            "600123456");
    }
}