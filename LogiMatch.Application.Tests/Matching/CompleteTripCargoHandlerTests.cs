using LogiMatch.Application.Matching;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.Matching;
public class CompleteTripCargoHandlerTests
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenTripCargoDoesNotExist()
    {
        var db = await CreateValidDatabase();
        var handler = new CompleteTripCargoHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(Guid.NewGuid()));

        Assert.Equal(
            "The specified trip cargo does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripDoesNotExist()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);
        request.AssignToTrip();

        var tripCargo = new TripCargo(
            Guid.NewGuid(),
            request.Id,
            100m,
            1m);

        db.TransportRequests.Update(request);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var handler = new CompleteTripCargoHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(tripCargo.Id));

        Assert.Equal(
            "The trip associated with the trip cargo does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenRequestDoesNotExist()
    {
        var db = await CreateValidDatabase();

        var trip = CreateTrip(db);

        var tripCargo = new TripCargo(
            trip.Id,
            Guid.NewGuid(),
            100m,
            1m);

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var handler = new CompleteTripCargoHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(tripCargo.Id));

        Assert.Equal(
            "The transport request associated with the trip cargo does not exist.",
            exception.Message);
    }

    [Theory]
    [InlineData(TripStatus.Published)]
    [InlineData(TripStatus.Completed)]
    [InlineData(TripStatus.Cancelled)]
    public async Task Handle_ShouldThrow_WhenTripIsNotInProgress(
        TripStatus status)
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);
        request.AssignToTrip();

        var trip = CreateTrip(db);

        if (status == TripStatus.Completed)
        {
            trip.Start();
            trip.Complete();
        }
        else if (status == TripStatus.Cancelled)
        {
            trip.Cancel();
        }

        var tripCargo = CreateReservedTripCargo(
            trip,
            request,
            100m,
            1m);

        db.TransportRequests.Update(request);
        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var handler = new CompleteTripCargoHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(tripCargo.Id));

        Assert.Equal(
            "The trip must be in progress before completing trip cargo.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldCompleteTripCargoAndRequest()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);
        request.AssignToTrip();

        var trip = CreateTrip(db);
        trip.ReserveCapacity(100m, 1m);

        var tripCargo = CreateReservedTripCargo(
            trip,
            request,
            100m,
            1m);

        db.TransportRequests.Update(request);
        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);

        // Preparar el estado previo al CompleteTripCargoHandler
        trip.Start();
        tripCargo.Start();
        request.Start();

        await db.SaveChangesAsync();

        var handler = new CompleteTripCargoHandler(db);

        await handler.Handle(tripCargo.Id);

        Assert.Equal(
            TripCargoStatus.Completed,
            tripCargo.Status);

        Assert.Equal(
            TransportRequestStatus.Completed,
            request.Status);
    }
    [Fact]
    public async Task Handle_ShouldThrow_WhenTripCargoIsAlreadyCompleted()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);
        request.AssignToTrip();
        request.Start();

        var trip = CreateTrip(db);
        trip.Start();

        var tripCargo = CreateReservedTripCargo(
            trip,
            request,
            100m,
            1m);
        tripCargo.Start();
        tripCargo.Complete();

        db.TransportRequests.Update(request);
        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var handler = new CompleteTripCargoHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(tripCargo.Id));

        Assert.Equal(
            "Only in-progress trip cargo can be completed.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripCargoIsCancelled()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);
        request.AssignToTrip();

        var trip = CreateTrip(db);
        trip.Start();

        var tripCargo = CreateReservedTripCargo(
            trip,
            request,
            100m,
            1m);
        tripCargo.Cancel();

        db.TransportRequests.Update(request);
        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var handler = new CompleteTripCargoHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(tripCargo.Id));

        Assert.Equal(
            "Only in-progress trip cargo can be completed.",
            exception.Message);
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

        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Locations.AddRange(origin, destination);

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

