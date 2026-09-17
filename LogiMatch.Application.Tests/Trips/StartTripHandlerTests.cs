using LogiMatch.Application.Trips;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using LogiMatch.Application.Tests.Common;
using Xunit;

namespace LogiMatch.Application.Tests.Trips;

public class StartTripHandlerTests
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenTripDoesNotExist()
    {
        var db = TestDbContextFactory.Create();
        var handler = new StartTripHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(Guid.NewGuid()));

        Assert.Equal(
            "The specified trip does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenReservedTripCargoRequestDoesNotExist()
    {
        var db = await CreateValidDatabase();

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        var tripCargo = new TripCargo(
            trip.Id,
            Guid.NewGuid(),
            100m,
            1m);

        db.TripCargos.Add(tripCargo);

        await db.SaveChangesAsync();

        var handler = new StartTripHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(trip.Id));

        Assert.Equal(
            "The transport request associated with the trip cargo does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTransportRequestIsNotAccepted()
    {
        var db = await CreateValidDatabase();

        var request = CreateRequest(
            TransportRequestStatus.Published);

        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        var tripCargo = new TripCargo(
            trip.Id,
            request.Id,
            100m,
            1m);

        db.TripCargos.Add(tripCargo);

        await db.SaveChangesAsync();

        var handler = new StartTripHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(trip.Id));

        Assert.Equal(
            "A reserved trip cargo must belong to an accepted transport request.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripIsNotPublished()
    {
        var db = await CreateValidDatabase();

        var request = CreateAcceptedRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        trip.Start();

        db.Trips.Add(trip);

        var tripCargo = new TripCargo(
            trip.Id,
            request.Id,
            100m,
            1m);

        db.TripCargos.Add(tripCargo);

        await db.SaveChangesAsync();

        var handler = new StartTripHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(trip.Id));

        Assert.Equal(
            "Only published trips can be started.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldStartTrip_WhenThereAreNoCargos()
    {
        var db = await CreateValidDatabase();

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new StartTripHandler(db);

        await handler.Handle(trip.Id);

        var savedTrip = db.Trips.Single();

        Assert.Equal(TripStatus.InProgress, savedTrip.Status);
    }

    [Fact]
    public async Task Handle_ShouldStartTripCargoAndRequest()
    {
        var db = await CreateValidDatabase();

        var request = CreateAcceptedRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        var tripCargo = new TripCargo(
            trip.Id,
            request.Id,
            100m,
            1m);

        db.TripCargos.Add(tripCargo);

        await db.SaveChangesAsync();

        var handler = new StartTripHandler(db);

        await handler.Handle(trip.Id);

        var savedTrip = db.Trips.Single();
        var savedCargo = db.TripCargos.Single();
        var savedRequest = db.TransportRequests.Single();

        Assert.Equal(TripStatus.InProgress, savedTrip.Status);
        Assert.Equal(TripCargoStatus.InProgress, savedCargo.Status);
        Assert.Equal(
            TransportRequestStatus.InProgress,
            savedRequest.Status);
    }

    [Fact]
    public async Task Handle_ShouldStartAllReservedCargosAndRequests()
    {
        var db = await CreateValidDatabase();

        var request1 = CreateAcceptedRequest(db);
        var request2 = CreateAcceptedRequest(db);

        db.TransportRequests.AddRange(request1, request2);

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        var cargo1 = new TripCargo(
            trip.Id,
            request1.Id,
            100m,
            1m);

        var cargo2 = new TripCargo(
            trip.Id,
            request2.Id,
            200m,
            2m);

        db.TripCargos.AddRange(cargo1, cargo2);

        await db.SaveChangesAsync();

        var handler = new StartTripHandler(db);

        await handler.Handle(trip.Id);

        var savedTrip = db.Trips.Single();
        var savedCargos = db.TripCargos.ToList();
        var savedRequests = db.TransportRequests.ToList();

        Assert.Equal(TripStatus.InProgress, savedTrip.Status);

        Assert.All(
            savedCargos,
            cargo => Assert.Equal(
                TripCargoStatus.InProgress,
                cargo.Status));

        Assert.All(
            savedRequests,
            request => Assert.Equal(
                TransportRequestStatus.InProgress,
                request.Status));
    }

    [Fact]
    public async Task Handle_ShouldIgnoreCancelledTripCargo()
    {
        var db = await CreateValidDatabase();

        var request = CreateAcceptedRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        var tripCargo = new TripCargo(
            trip.Id,
            request.Id,
            100m,
            1m);

        tripCargo.Cancel();

        db.TripCargos.Add(tripCargo);

        await db.SaveChangesAsync();

        var handler = new StartTripHandler(db);

        await handler.Handle(trip.Id);

        var savedTrip = db.Trips.Single();
        var savedCargo = db.TripCargos.Single();
        var savedRequest = db.TransportRequests.Single();

        Assert.Equal(TripStatus.InProgress, savedTrip.Status);
        Assert.Equal(TripCargoStatus.Cancelled, savedCargo.Status);

        // La solicitud asociada no se inicia porque su TripCargo está cancelado.
        Assert.Equal(
            TransportRequestStatus.Accepted,
            savedRequest.Status);
    }

    [Fact]
    public async Task Handle_ShouldIgnoreCompletedTripCargo()
    {
        var db = await CreateValidDatabase();

        var request = CreateAcceptedRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        var tripCargo = new TripCargo(
            trip.Id,
            request.Id,
            100m,
            1m);

        tripCargo.Start();
        tripCargo.Complete();

        db.TripCargos.Add(tripCargo);

        await db.SaveChangesAsync();

        var handler = new StartTripHandler(db);

        await handler.Handle(trip.Id);

        var savedTrip = db.Trips.Single();
        var savedCargo = db.TripCargos.Single();
        var savedRequest = db.TransportRequests.Single();

        Assert.Equal(TripStatus.InProgress, savedTrip.Status);
        Assert.Equal(TripCargoStatus.Completed, savedCargo.Status);

        Assert.Equal(
            TransportRequestStatus.Accepted,
            savedRequest.Status);
    }

    private static async Task<TestDbContext> CreateValidDatabase()
    {
        var db = TestDbContextFactory.Create();

        var transporter = CreateTransporter();
        var vehicle = CreateVehicle(transporter.Id);
        var origin = CreateLocation();
        var destination = CreateLocation();

        var availability = new VehicleAvailability(
            vehicle.Id,
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddDays(10));

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

    private static TransportRequest CreateAcceptedRequest(TestDbContext db)
    {
        var locations = db.Locations.ToList();

        var request = new TransportRequest(
            Guid.NewGuid(),
            locations[0].Id,
            locations[1].Id,
            DateTime.UtcNow.AddDays(1),
            null);

        request.Publish();
        request.AssignToTrip();

        return request;
    }

    private static TransportRequest CreateRequest(
        TransportRequestStatus status)
    {
        var request = new TransportRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            null);

        if (status == TransportRequestStatus.Published)
            request.Publish();

        return request;
    }

    private static TransporterProfile CreateTransporter()
    {
        return new TransporterProfile(Guid.NewGuid());
    }

    private static Vehicle CreateVehicle(Guid transporterProfileId)
    {
        return new Vehicle(
            transporterProfileId,
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
    }

    private static Location CreateLocation()
    {
        return new Location(
            "Calle Test 1",
            "Sevilla",
            "41001",
            "España",
            37.3772,
            -5.9869);
    }
}