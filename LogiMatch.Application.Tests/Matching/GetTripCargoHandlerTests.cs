using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Matching;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.Matching;

public class GetTripCargoHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnTripCargo_WhenTransporterOwnsTheTrip()
    {
        var db = await CreateValidDatabase();
        var transporter = db.TransporterProfiles.Single();
        var currentUserService = new MockCurrentUserService(transporter.UserId);

        var request = await CreateValidRequest(db);
        var trip = CreateTrip(db);
        var tripCargo = CreateReservedTripCargo(trip, request, 100m, 1m);

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var handler = new GetTripCargoHandler(db, currentUserService);

        var result = await handler.Handle(tripCargo.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnTripCargo_WhenCustomerOwnsTheRequest()
    {
        var db = await CreateValidDatabase();
        var request = await CreateValidRequest(db);
        var currentUserService = new MockCurrentUserService(request.CustomerId);

        var trip = CreateTrip(db);
        var tripCargo = CreateReservedTripCargo(trip, request, 100m, 1m);

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var handler = new GetTripCargoHandler(db, currentUserService);

        var result = await handler.Handle(tripCargo.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserDoesNotOwnTripOrRequest()
    {
        var db = await CreateValidDatabase();
        var otherUserId = Guid.NewGuid();
        var currentUserService = new MockCurrentUserService(otherUserId);

        var request = await CreateValidRequest(db);
        var trip = CreateTrip(db);
        var tripCargo = CreateReservedTripCargo(trip, request, 100m, 1m);

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var handler = new GetTripCargoHandler(db, currentUserService);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(tripCargo.Id));

        Assert.Equal(
            "You do not have permission to view this trip cargo.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenTripCargoDoesNotExist()
    {
        var db = await CreateValidDatabase();
        var currentUserService = new MockCurrentUserService(Guid.NewGuid());

        var handler = new GetTripCargoHandler(db, currentUserService);

        var result = await handler.Handle(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAll_ShouldReturnOnlyTransporterTrips()
    {
        var db = await CreateValidDatabase();
        var transporter = db.TransporterProfiles.Single();
        var currentUserService = new MockCurrentUserService(transporter.UserId);

        var request = await CreateValidRequest(db);
        var trip = CreateTrip(db);
        var tripCargo = CreateReservedTripCargo(trip, request, 100m, 1m);

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var handler = new GetTripCargoHandler(db, currentUserService);

        var result = await handler.HandleAll();

        Assert.IsType<List<dynamic>>(result);
    }

    [Fact]
    public async Task HandleAll_ShouldReturnOnlyCustomerRequests()
    {
        var db = await CreateValidDatabase();
        var request = await CreateValidRequest(db);
        var currentUserService = new MockCurrentUserService(request.CustomerId);

        var trip = CreateTrip(db);
        var tripCargo = CreateReservedTripCargo(trip, request, 100m, 1m);

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var handler = new GetTripCargoHandler(db, currentUserService);

        var result = await handler.HandleAll();

        Assert.IsType<List<dynamic>>(result);
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