using LogiMatch.Api.Controllers;
using LogiMatch.Application.TransporterProfiles;
using LogiMatch.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogiMatch.Api.Tests.Controllers;

public class TransporterProfilesControllerTests
{
    [Fact]
    public async Task Get_WhenProfileDoesNotExist_ShouldReturnNotFound()
    {
        var user = CreateUser();
        var mockDb = CreateMockDbContext();
        var mockCurrentUserService = new MockCurrentUserService(user.Id);
        var handler = new GetTransporterProfileHandler(mockDb, mockCurrentUserService);

        var controller = new TransporterProfilesController(
            new CreateTransporterProfileHandler(mockDb, mockCurrentUserService),
            handler,
            mockCurrentUserService,
            mockDb);

        var result = await controller.Get(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_WhenProfileExistsAndUserIsNotOwner_ShouldReturnPublicProfile()
    {
        using var db = TestDbContextFactory.Create();

        var profileOwner = CreateUser();
        var currentUser = CreateUser();

        db.Users.Add(profileOwner);
        db.Users.Add(currentUser);
        await db.SaveChangesAsync();

        var profile = new TransporterProfile(profileOwner.Id);
        db.TransporterProfiles.Add(profile);
        await db.SaveChangesAsync();

        var handler = new GetTransporterProfileHandler(db, new MockCurrentUserService(currentUser.Id));
        var createHandler = new CreateTransporterProfileHandler(db, new MockCurrentUserService(currentUser.Id));
        var mockCurrentUserService = new MockCurrentUserService(currentUser.Id);

        var controller = new TransporterProfilesController(
            createHandler,
            handler,
            mockCurrentUserService,
            db);

        var result = await controller.Get(profile.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedProfile = Assert.IsType<PublicTransporterProfileDto>(okResult.Value);

        Assert.Equal(profile.Id, returnedProfile.Id);
        Assert.Equal(profileOwner.FirstName, returnedProfile.FirstName);
        Assert.Equal(profileOwner.LastName, returnedProfile.LastName);
        // Verificar que no contiene datos sensibles
        Assert.DoesNotContain("Email", returnedProfile.GetType().GetProperties().Select(p => p.Name));
    }

    [Fact]
    public async Task Get_WhenProfileExistsAndUserIsOwner_ShouldReturnDetailedProfile()
    {
        using var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var company = CreateCompany();

        db.Users.Add(user);
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var profile = new TransporterProfile(user.Id) { CompanyId = company.Id };
        db.TransporterProfiles.Add(profile);
        await db.SaveChangesAsync();

        var handler = new GetTransporterProfileHandler(db, new MockCurrentUserService(user.Id));
        var createHandler = new CreateTransporterProfileHandler(db, new MockCurrentUserService(user.Id));
        var mockCurrentUserService = new MockCurrentUserService(user.Id);

        var controller = new TransporterProfilesController(
            createHandler,
            handler,
            mockCurrentUserService,
            db);

        var result = await controller.Get(profile.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedProfile = Assert.IsType<DetailedTransporterProfileDto>(okResult.Value);

        Assert.Equal(profile.Id, returnedProfile.Id);
        Assert.Equal(user.Id, returnedProfile.UserId);
        Assert.Equal(user.Email, returnedProfile.Email);
        Assert.Equal(user.Phone, returnedProfile.Phone);
        Assert.Equal(company.TaxId, returnedProfile.TaxId);
    }

    [Fact]
    public async Task GetAll_ShouldBeAnonymousAccessible()
    {
        using var db = TestDbContextFactory.Create();

        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var profile = new TransporterProfile(user.Id);
        db.TransporterProfiles.Add(profile);
        await db.SaveChangesAsync();

        var handler = new GetTransporterProfileHandler(db, new MockCurrentUserService(Guid.NewGuid()));
        var createHandler = new CreateTransporterProfileHandler(db, new MockCurrentUserService(Guid.NewGuid()));
        var mockCurrentUserService = new MockCurrentUserService(Guid.NewGuid());

        var controller = new TransporterProfilesController(
            createHandler,
            handler,
            mockCurrentUserService,
            db);

        var result = await controller.GetAll(null, null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var profiles = Assert.IsType<List<PublicTransporterProfileDto>>(okResult.Value);

        Assert.NotEmpty(profiles);
        Assert.All(profiles, p => Assert.DoesNotContain("Email", p.GetType().GetProperties().Select(pr => pr.Name)));
    }

    [Fact]
    public async Task Get_WhenAnonymousUser_ShouldReturnPublicProfile()
    {
        using var db = TestDbContextFactory.Create();

        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var profile = new TransporterProfile(user.Id);
        db.TransporterProfiles.Add(profile);
        await db.SaveChangesAsync();

        var handler = new GetTransporterProfileHandler(db, new MockCurrentUserService(Guid.Empty));
        var createHandler = new CreateTransporterProfileHandler(db, new MockCurrentUserService(Guid.Empty));
        var mockCurrentUserService = new MockCurrentUserService(Guid.Empty);

        var controller = new TransporterProfilesController(
            createHandler,
            handler,
            mockCurrentUserService,
            db);

        var result = await controller.Get(profile.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedProfile = Assert.IsType<PublicTransporterProfileDto>(okResult.Value);

        Assert.Equal(profile.Id, returnedProfile.Id);
        Assert.DoesNotContain("Email", returnedProfile.GetType().GetProperties().Select(p => p.Name));
    }

    [Fact]
    public async Task Create_WhenCommandIsValid_ShouldReturnCreatedWithProfileId()
    {
        using var db = TestDbContextFactory.Create();

        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new GetTransporterProfileHandler(db, new MockCurrentUserService(user.Id));
        var createHandler = new CreateTransporterProfileHandler(db, new MockCurrentUserService(user.Id));
        var mockCurrentUserService = new MockCurrentUserService(user.Id);

        var controller = new TransporterProfilesController(
            createHandler,
            handler,
            mockCurrentUserService,
            db);

        var command = new CreateTransporterProfileCommand(user.Id, null);
        var result = await controller.Create(command);

        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.NotNull(createdResult.Value);
    }

    private static User CreateUser()
    {
        return new User(
            email: $"user-{Guid.NewGuid()}@example.com",
            firstName: "John",
            lastName: "Doe",
            passwordHash: "hash")
        {
            Phone = "555-1234"
        };
    }

    private static Company CreateCompany()
    {
        return new Company(
            name: $"Company-{Guid.NewGuid()}",
            taxId: "12345678A");
    }

    private static IApplicationDbContext CreateMockDbContext()
    {
        return TestDbContextFactory.Create();
    }
}