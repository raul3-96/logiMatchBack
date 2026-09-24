using LogiMatch.Application.Common.Services;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.Trips;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.Trips;

public class GetTripHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnTrip_WhenCurrentUserIsProfileOwner()
    {
        var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var trip = CreateTrip(db);

        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var currentUserService = new MockCurrentUserService(
            transporter.UserId);

        var accessService = new TripManagementAccessService(
            db,
            currentUserService);

        var handler = new GetTripHandler(
            db,
            accessService);

        var result = await handler.Handle(
            new GetTripCommand
            {
                TripId = trip.Id
            });

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrip_WhenCurrentUserIsCompanyOwner()
    {
        var db = await CreateValidDatabaseWithCompany();

        var company = db.Companies.Single();
        var trip = CreateTrip(db);

        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var currentUserService = new MockCurrentUserService(
            company.OwnerUserId);

        var accessService = new TripManagementAccessService(
            db,
            currentUserService);

        var handler = new GetTripHandler(
            db,
            accessService);

        var result = await handler.Handle(
            new GetTripCommand
            {
                TripId = trip.Id
            });

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrip_WhenCurrentUserIsCompanyAdmin()
    {
        var db = await CreateValidDatabaseWithCompany();

        var company = db.Companies.Single();
        var adminUserId = Guid.NewGuid();

        db.CompanyMembers.Add(
            new CompanyMember(
                company.Id,
                adminUserId,
                CompanyMemberRole.Admin));

        await db.SaveChangesAsync();

        var trip = CreateTrip(db);

        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var currentUserService = new MockCurrentUserService(
            adminUserId);

        var accessService = new TripManagementAccessService(
            db,
            currentUserService);

        var handler = new GetTripHandler(
            db,
            accessService);

        var result = await handler.Handle(
            new GetTripCommand
            {
                TripId = trip.Id
            });

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenCurrentUserCannotAccessTrip()
    {
        var db = await CreateValidDatabase();

        var trip = CreateTrip(db);

        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var currentUserService = new MockCurrentUserService(Guid.NewGuid());
        var accessService = new TripManagementAccessService(
            db,
            currentUserService);

        var handler = new GetTripHandler(
            db,
            accessService);

        var result = await handler.Handle(
            new GetTripCommand
            {
                TripId = trip.Id
            });

        Assert.Null(result);
    }

    private static async Task<TestDbContext> CreateValidDatabase()
    {
        var db = TestDbContextFactory.Create();

        var transporter = new TransporterProfile(Guid.NewGuid());

        var vehicle = new Vehicle(
            transporter.Id,
            VehicleType.Van,
            "Mercedes",
            "Sprinter",
            "1234ABC",
            1500m,
            10m,
            6m,
            2m,
            2.5m,
            true,
            false);

        var origin = CreateLocation();
        var destination = CreateLocation(
            "Calle Test 2",
            37.3886,
            -5.9953);

        var availability = new VehicleAvailability(
            vehicle.Id,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(10));

        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Locations.AddRange(origin, destination);
        db.VehicleAvailabilities.Add(availability);

        await db.SaveChangesAsync();

        return db;
    }

    private static async Task<TestDbContext> CreateValidDatabaseWithCompany()
    {
        var db = TestDbContextFactory.Create();

        var ownerUserId = Guid.NewGuid();

        var company = new Company(
            "Transport Company",
            "ESB12345678",
            "company@example.com",
            "600123456",
            ownerUserId);

        var transporter = new TransporterProfile(
            Guid.NewGuid(),
            company.Id);

        var vehicle = new Vehicle(
            transporter.Id,
            VehicleType.Van,
            "Mercedes",
            "Sprinter",
            "1234ABC",
            1500m,
            10m,
            6m,
            2m,
            2.5m,
            true,
            false);

        var origin = CreateLocation();
        var destination = CreateLocation(
            "Calle Test 2",
            37.3886,
            -5.9953);

        var availability = new VehicleAvailability(
            vehicle.Id,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(10));

        db.Companies.Add(company);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Locations.AddRange(origin, destination);
        db.VehicleAvailabilities.Add(availability);

        await db.SaveChangesAsync();

        return db;
    }

    private static Trip CreateTrip(TestDbContext db)
    {
        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();
        var locations = db.Locations.ToList();

        return new Trip(
            transporter.Id,
            vehicle.Id,
            locations[0].Id,
            locations[1].Id,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(8),
            1000m,
            5m);
    }

    private static Location CreateLocation(
        string address = "Calle Test 1",
        double latitude = 37.3772,
        double longitude = -5.9869)
    {
        return new Location(
            address,
            "Sevilla",
            "41001",
            "España",
            latitude,
            longitude);
    }
}