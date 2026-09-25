using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.TransporterProfiles;
using LogiMatch.Domain.Entities;
using System.Numerics;
using Xunit;

namespace LogiMatch.Application.Tests.TransporterProfiles;

public class GetTransporterProfileHandlerTests
{
    [Fact]
    public async Task Handle_WhenProfileDoesNotExist_ShouldReturnNull()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetTransporterProfileHandler(db, new MockCurrentUserService(Guid.NewGuid()));

        //var result = await handler.Handle(Guid.NewGuid());

        //Assert.Null(result);
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(Guid.NewGuid()));

        Assert.Equal(
            "The specified transporter profile does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenProfileExists_ShouldReturnPublicProfile()
    {
        using var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var company = CreateCompany();
        db.Users.Add(user);
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var profile = new TransporterProfile(user.Id, company.Id);
        db.TransporterProfiles.Add(profile);
        await db.SaveChangesAsync();

        var handler = new GetTransporterProfileHandler(db, new MockCurrentUserService(Guid.NewGuid()));
        var result = await handler.Handle(profile.Id);

        Assert.NotNull(result);
        Assert.Equal(profile.Id, result.Id);
        Assert.Equal(user.FirstName, result.FirstName);
        Assert.Equal(user.LastName, result.LastName);
        Assert.Equal(company.Name, result.CompanyName);
    }

    [Fact]
    public async Task Handle_WhenProfileExistsWithoutCompany_ShouldReturnPublicProfileWithoutCompanyName()
    {
        using var db = TestDbContextFactory.Create();

        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var profile = new TransporterProfile(user.Id);
        db.TransporterProfiles.Add(profile);
        await db.SaveChangesAsync();

        var handler = new GetTransporterProfileHandler(db, new MockCurrentUserService(Guid.NewGuid()));
        var result = await handler.Handle(profile.Id);

        Assert.NotNull(result);
        Assert.Equal(profile.Id, result.Id);
        Assert.Equal(user.FirstName, result.FirstName);
        Assert.Equal(user.LastName, result.LastName);
        Assert.Null(result.CompanyName);
    }

    [Fact]
    public async Task Handle_ShouldNotExposeEmailOrPhone()
    {
        using var db = TestDbContextFactory.Create();

        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var profile = new TransporterProfile(user.Id);
        db.TransporterProfiles.Add(profile);
        await db.SaveChangesAsync();

        var handler = new GetTransporterProfileHandler(db, new MockCurrentUserService(Guid.NewGuid()));
        var result = await handler.Handle(profile.Id);

        Assert.NotNull(result);
        // Verificar que PublicTransporterProfileDto no tiene Email ni Phone
        var dto = result;
        Assert.Equal(string.Empty, result.Email);
        Assert.Null(result.Phone);
        Assert.Null(result.TaxId);        
    }

    [Fact]
    public async Task HandleDetailed_WhenProfileDoesNotExist_ShouldReturnNull()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetTransporterProfileHandler(db, new MockCurrentUserService(Guid.NewGuid()));

        var result = await handler.HandleDetailed(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleDetailed_WhenProfileExists_ShouldReturnDetailedProfileWithSensitiveData()
    {
        using var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var company = CreateCompany();
        db.Users.Add(user);
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var profile = new TransporterProfile(user.Id, company.Id);
        db.TransporterProfiles.Add(profile);
        await db.SaveChangesAsync();

        var handler = new GetTransporterProfileHandler(db, new MockCurrentUserService(Guid.NewGuid()));
        var result = await handler.HandleDetailed(profile.Id);

        Assert.NotNull(result);
        Assert.Equal(profile.Id, result.Id);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.FirstName, result.FirstName);
        Assert.Equal(user.LastName, result.LastName);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.Phone, result.Phone);
        Assert.Equal(company.Name, result.CompanyName);
        Assert.Equal(company.TaxId, result.TaxId);
    }

    [Fact]
    public async Task HandleDetailed_ShouldExposeEmailPhoneAndTaxId()
    {
        using var db = TestDbContextFactory.Create();

        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var profile = new TransporterProfile(user.Id);
        db.TransporterProfiles.Add(profile);
        await db.SaveChangesAsync();

        var handler = new GetTransporterProfileHandler(db, new MockCurrentUserService(Guid.NewGuid()));
        var result = await handler.HandleDetailed(profile.Id);

        Assert.NotNull(result);
        // Verificar que DetailedTransporterProfileDto tiene Email, Phone, etc
        var dto = result;
        Assert.Contains("Email", dto.GetType().GetProperties().Select(p => p.Name));
        Assert.Contains("Phone", dto.GetType().GetProperties().Select(p => p.Name));
        Assert.Contains("TaxId", dto.GetType().GetProperties().Select(p => p.Name));
    }

    [Fact]
    public async Task HandleAll_ShouldReturnPublicProfiles()
    {
        using var db = TestDbContextFactory.Create();

        var user1 = CreateUser();
        var user2 = CreateUser();
        db.Users.Add(user1);
        db.Users.Add(user2);
        await db.SaveChangesAsync();

        var profile1 = new TransporterProfile(user1.Id);
        var profile2 = new TransporterProfile(user2.Id);
        db.TransporterProfiles.Add(profile1);
        db.TransporterProfiles.Add(profile2);
        await db.SaveChangesAsync();

        var handler = new GetTransporterProfileHandler(db, new MockCurrentUserService(Guid.NewGuid()));
        var results = await handler.HandleAll();

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.DoesNotContain("Email", r.GetType().GetProperties().Select(p => p.Name)));
    }

    [Fact]
    public async Task HandleAll_WithUserIdFilter_ShouldReturnFilteredProfiles()
    {
        using var db = TestDbContextFactory.Create();

        var user1 = CreateUser();
        var user2 = CreateUser();
        db.Users.Add(user1);
        db.Users.Add(user2);
        await db.SaveChangesAsync();

        var profile1 = new TransporterProfile(user1.Id);
        var profile2 = new TransporterProfile(user2.Id);
        db.TransporterProfiles.Add(profile1);
        db.TransporterProfiles.Add(profile2);
        await db.SaveChangesAsync();

        var handler = new GetTransporterProfileHandler(db, new MockCurrentUserService(Guid.NewGuid()));
        var results = await handler.HandleAll(userId: user1.Id);

        Assert.Single(results);
        Assert.Equal(user1.FirstName, results[0].FirstName);
    }

    [Fact]
    public async Task HandleAll_WithCompanyIdFilter_ShouldReturnFilteredProfiles()
    {
        using var db = TestDbContextFactory.Create();

        var user1 = CreateUser();
        var user2 = CreateUser();
        var company1 = CreateCompany();
        var company2 = CreateCompany();

        db.Users.Add(user1);
        db.Users.Add(user2);
        db.Companies.Add(company1);
        db.Companies.Add(company2);
        await db.SaveChangesAsync();

        var profile1 = new TransporterProfile(user1.Id, company1.Id);
        var profile2 = new TransporterProfile(user2.Id, company2.Id);
        db.TransporterProfiles.Add(profile1);
        db.TransporterProfiles.Add(profile2);
        await db.SaveChangesAsync();

        var handler = new GetTransporterProfileHandler(db, new MockCurrentUserService(Guid.NewGuid()));
        var results = await handler.HandleAll(companyId: company1.Id);

        Assert.Single(results);
        Assert.Equal(company1.Name, results[0].CompanyName);
    }

    [Fact]
    public async Task HandleAll_ShouldReturnOrderedByCreatedAtDescending()
    {
        using var db = TestDbContextFactory.Create();

        var user1 = CreateUser();
        var user2 = CreateUser();
        db.Users.Add(user1);
        db.Users.Add(user2);
        await db.SaveChangesAsync();

        var profile1 = new TransporterProfile(user1.Id);
        var profile2 = new TransporterProfile(user2.Id);
        db.TransporterProfiles.Add(profile1);
        await db.SaveChangesAsync();

        // Simular que profile2 se creó después
        await Task.Delay(100);

        db.TransporterProfiles.Add(profile2);
        await db.SaveChangesAsync();

        var handler = new GetTransporterProfileHandler(db, new MockCurrentUserService(Guid.NewGuid()));
        var results = await handler.HandleAll();

        Assert.Equal(2, results.Count);
        Assert.Equal(profile2.Id, results[0].Id);
        Assert.Equal(profile1.Id, results[1].Id);
    }

    private static User CreateUser()
    {
        User user = new User(
            email: $"user-{Guid.NewGuid()}@example.com",
            firstName: "John",
            lastName: "Doe",
            phone: "555-1234");
        user.SetPasswordHash("hashedpassword");
        return user;
    }

    private static Company CreateCompany()
    {
        return new Company(
            name: $"Company-{Guid.NewGuid()}",
            taxId: "12345678A",
            email: $"company-{Guid.NewGuid()}@example.com",
            phone: "555-1234", Guid.NewGuid());
    }
}