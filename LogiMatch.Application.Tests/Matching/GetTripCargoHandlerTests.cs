using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Services;
using LogiMatch.Application.Matching;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.Matching;

public class GetTripCargoHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNull_WhenTripCargoDoesNotExist()
    {
        var db = await CreateValidDatabase();

        var currentUserService = new MockCurrentUserService(Guid.NewGuid());
        var accessService = new TripManagementAccessService(
            db,
            currentUserService);

        var handler = new GetTripCargoHandler(
            db,
            currentUserService,
            accessService);

        var result = await handler.Handle(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ShouldAllow_WhenCurrentUserIsCustomer()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);
        var trip = CreateTrip(db);
        var tripCargo = CreateReservedTripCargo(
            trip,
            request,
            100m,
            1m);

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var currentUserService = new MockCurrentUserService(
            request.CustomerId);

        var accessService = new TripManagementAccessService(
            db,
            currentUserService);

        var handler = new GetTripCargoHandler(
            db,
            currentUserService,
            accessService);

        var result = await handler.Handle(tripCargo.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_ShouldAllow_WhenCurrentUserIsProfileOwner()
    {
        var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var request = await CreateValidRequest(db);
        var trip = CreateTrip(db);
        var tripCargo = CreateReservedTripCargo(
            trip,
            request,
            100m,
            1m);

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var currentUserService = new MockCurrentUserService(
            transporter.UserId);

        var accessService = new TripManagementAccessService(
            db,
            currentUserService);

        var handler = new GetTripCargoHandler(
            db,
            currentUserService,
            accessService);

        var result = await handler.Handle(tripCargo.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_ShouldAllow_WhenCurrentUserIsCompanyOwner()
    {
        var db = await CreateValidDatabaseWithCompany();

        var company = db.Companies.Single();
        var request = await CreateValidRequest(db);
        var trip = CreateTrip(db);
        var tripCargo = CreateReservedTripCargo(
            trip,
            request,
            100m,
            1m);

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var currentUserService = new MockCurrentUserService(
            company.OwnerUserId);

        var accessService = new TripManagementAccessService(
            db,
            currentUserService);

        var handler = new GetTripCargoHandler(
            db,
            currentUserService,
            accessService);

        var result = await handler.Handle(tripCargo.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_ShouldAllow_WhenCurrentUserIsCompanyAdmin()
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

        var request = await CreateValidRequest(db);
        var trip = CreateTrip(db);
        var tripCargo = CreateReservedTripCargo(
            trip,
            request,
            100m,
            1m);

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var currentUserService = new MockCurrentUserService(
            adminUserId);

        var accessService = new TripManagementAccessService(
            db,
            currentUserService);

        var handler = new GetTripCargoHandler(
            db,
            currentUserService,
            accessService);

        var result = await handler.Handle(tripCargo.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenCurrentUserCannotViewTripCargo()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);
        var trip = CreateTrip(db);
        var tripCargo = CreateReservedTripCargo(
            trip,
            request,
            100m,
            1m);

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var currentUserService = new MockCurrentUserService(Guid.NewGuid());
        var accessService = new TripManagementAccessService(
            db,
            currentUserService);

        var handler = new GetTripCargoHandler(
            db,
            currentUserService,
            accessService);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(tripCargo.Id));

        Assert.Equal(
            "You do not have permission to view this trip cargo.",
            exception.Message);
    }

    [Fact]
    public async Task HandleAll_ShouldReturnOnlyCustomerTripCargos()
    {
        var db = await CreateValidDatabase();

        var customerRequest = await CreateValidRequest(db);
        var otherRequest = await CreateValidRequest(db);

        var trip1 = CreateTrip(db);
        var trip2 = CreateTrip(db);

        var customerTripCargo = CreateReservedTripCargo(
            trip1,
            customerRequest,
            100m,
            1m);

        var otherTripCargo = CreateReservedTripCargo(
            trip2,
            otherRequest,
            150m,
            1.5m);

        db.Trips.AddRange(trip1, trip2);
        db.TripCargos.AddRange(customerTripCargo, otherTripCargo);
        await db.SaveChangesAsync();

        var currentUserService = new MockCurrentUserService(
            customerRequest.CustomerId);

        var accessService = new TripManagementAccessService(
            db,
            currentUserService);

        var handler = new GetTripCargoHandler(
            db,
            currentUserService,
            accessService);

        var result = await handler.HandleAll();

        var list = ((System.Collections.IEnumerable)result)
            .Cast<object>()
            .ToList();

        Assert.Single(list);

        var itemType = list[0].GetType();

        Assert.Equal(
            customerTripCargo.Id,
            itemType.GetProperty("Id")!.GetValue(list[0]));
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

    private static async Task<TransportRequest> CreateValidRequest(
        TestDbContext db)
    {
        var locations = db.Locations.ToList();

        var request = new TransportRequest(
            Guid.NewGuid(),
            locations[0].Id,
            locations[1].Id,
            DateTime.UtcNow.AddDays(1).AddHours(1),
            null);

        var cargo = new Cargo(
            request.Id,
            "Test cargo",
            100m,
            1m,
            1,
            false,
            false);

        request.Publish();

        db.TransportRequests.Add(request);
        db.Cargos.Add(cargo);

        await db.SaveChangesAsync();

        return request;
    }

    private static Trip CreateTrip(TestDbContext db)
    {
        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();
        var locations = db.Locations.ToList();

        var departure = DateTime.UtcNow.AddDays(1);
        var arrival = departure.AddHours(8);

        return new Trip(
            transporter.Id,
            vehicle.Id,
            locations[0].Id,
            locations[1].Id,
            departure,
            arrival,
            1000m,
            5m);
    }

    private static TripCargo CreateReservedTripCargo(
        Trip trip,
        TransportRequest request,
        decimal weightKg,
        decimal volumeM3)
    {
        return new TripCargo(
            trip.Id,
            request.Id,
            weightKg,
            volumeM3);
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