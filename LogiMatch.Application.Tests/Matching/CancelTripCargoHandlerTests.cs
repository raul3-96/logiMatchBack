using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Application.Matching;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.Matching;

public class CancelTripCargoHandlerTests
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenTripCargoDoesNotExist()
    {
        var db = await CreateValidDatabase();
        var currentUserService = new MockCurrentUserService(Guid.NewGuid());
        var handler = new CancelTripCargoHandler(db, currentUserService);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(Guid.NewGuid()));

        Assert.Equal(
            "The specified trip cargo does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripDoesNotExist()
    {
        var db = await CreateValidDatabase();
        var currentUserService = new MockCurrentUserService(Guid.NewGuid());

        var request = await CreateValidRequest(db);
        var tripCargo = new TripCargo(
            Guid.NewGuid(),
            request.Id,
            100m,
            1m);

        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var handler = new CancelTripCargoHandler(db, currentUserService);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(tripCargo.Id));

        Assert.Equal(
            "The specified trip does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserDoesNotOwnTheRequest()
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

        var handler = new CancelTripCargoHandler(db, currentUserService);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(tripCargo.Id));

        Assert.Equal(
            "You do not have permission to cancel this trip cargo.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripIsNotPublished()
    {
        var db = await CreateValidDatabase();
        var request = await CreateValidRequest(db);
        
        var currentUserService = new MockCurrentUserService(request.CustomerId);

        var trip = CreateTrip(db);
        trip.Start();

        var tripCargo = CreateReservedTripCargo(trip, request, 100m, 1m);

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var handler = new CancelTripCargoHandler(db, currentUserService);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(tripCargo.Id));

        Assert.Equal(
            "Trip cargo can only be cancelled while the trip is published.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenRequestDoesNotExist()
    {
        var db = await CreateValidDatabase();
        var currentUserService = new MockCurrentUserService(Guid.NewGuid());

        var trip = CreateTrip(db);
        var tripCargo = new TripCargo(
            trip.Id,
            Guid.NewGuid(),
            100m,
            1m);

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var handler = new CancelTripCargoHandler(db, currentUserService);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(tripCargo.Id));

        Assert.Equal(
            "The specified transport request does not exist.",
            exception.Message);
    }

    [Theory]
    [InlineData(TransportRequestStatus.Published)]
    [InlineData(TransportRequestStatus.Matching)]
    [InlineData(TransportRequestStatus.Cancelled)]
    [InlineData(TransportRequestStatus.Expired)]
    public async Task Handle_ShouldThrow_WhenRequestIsNotAccepted(
        TransportRequestStatus status)
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);
        SetRequestStatus(request, status);

        var currentUserService = new MockCurrentUserService(request.CustomerId);

        var trip = CreateTrip(db);
        var tripCargo = CreateReservedTripCargo(trip, request, 100m, 1m);

        db.TransportRequests.Update(request);
        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var handler = new CancelTripCargoHandler(db, currentUserService);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(tripCargo.Id));

        Assert.Equal(
            "Only accepted transport requests can have a reserved trip cargo cancelled.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldCancelCargoReleaseCapacityAndReturnRequestToPublished()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);
        var currentUserService = new MockCurrentUserService(request.CustomerId);

        var trip = CreateTrip(
            db,
            availableWeightKg: 1000m,
            availableVolumeM3: 5m);

        trip.ReserveCapacity(300m, 2m);

        var tripCargo = CreateReservedTripCargo(
            trip,
            request,
            300m,
            2m);

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        request.AssignToTrip();
        db.TransportRequests.Update(request);
        await db.SaveChangesAsync();

        var handler = new CancelTripCargoHandler(db, currentUserService);

        await handler.Handle(tripCargo.Id);

        Assert.Equal(
            TripCargoStatus.Cancelled,
            tripCargo.Status);

        Assert.Equal(
            1000m,
            trip.AvailableWeightKg);

        Assert.Equal(
            5m,
            trip.AvailableVolumeM3);

        Assert.Equal(
            TransportRequestStatus.Published,
            request.Status);

        Assert.Null(request.Fulfillment);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripCargoIsAlreadyCancelled()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);
        var currentUserService = new MockCurrentUserService(request.CustomerId);
        
        request.AssignToTrip();

        var trip = CreateTrip(db);
        trip.ReserveCapacity(100m, 1m);

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

        var handler = new CancelTripCargoHandler(db, currentUserService);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(tripCargo.Id));

        Assert.Equal(
            "Only reserved trip cargo can be cancelled.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldNotDoubleReleaseCapacity_WhenCalledAfterCancellation()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);
        var currentUserService = new MockCurrentUserService(request.CustomerId);
        
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
        await db.SaveChangesAsync();

        var handler = new CancelTripCargoHandler(db, currentUserService);

        await handler.Handle(tripCargo.Id);

        Assert.Equal(1000m, trip.AvailableWeightKg);
        Assert.Equal(5m, trip.AvailableVolumeM3);

        // El segundo intento debe fallar antes de liberar capacidad.
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(tripCargo.Id));

        Assert.Equal(
            "Only accepted transport requests can have a reserved trip cargo cancelled.",
            exception.Message);

        Assert.Equal(1000m, trip.AvailableWeightKg);
        Assert.Equal(5m, trip.AvailableVolumeM3);
    }

    [Fact]
    public async Task Handle_ShouldNotCancel_WhenTripCargoIsInProgress()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);
        var currentUserService = new MockCurrentUserService(request.CustomerId);
        
        request.AssignToTrip();

        var trip = CreateTrip(db);
        trip.ReserveCapacity(100m, 1m);

        var tripCargo = CreateReservedTripCargo(
            trip,
            request,
            100m,
            1m);
        trip.Start();
        tripCargo.Start();

        db.TransportRequests.Update(request);
        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);
        await db.SaveChangesAsync();

        var handler = new CancelTripCargoHandler(db, currentUserService);

        // El Trip no está publicado, por lo que el handler debe detenerse antes
        // de intentar cancelar el TripCargo.
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(tripCargo.Id));

        Assert.Equal(
            "Trip cargo can only be cancelled while the trip is published.",
            exception.Message);

        Assert.Equal(
            TripCargoStatus.InProgress,
            tripCargo.Status);
    }

    [Fact]
    public async Task Handle_ShouldAllowCancellation_WhenPreviousReservationWasCancelled()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);
        var currentUserService = new MockCurrentUserService(request.CustomerId);
        
        request.AssignToTrip();

        var trip = CreateTrip(db);
        trip.ReserveCapacity(100m, 1m);

        var previousCargo = CreateReservedTripCargo(
            trip,
            request,
            100m,
            1m);
        previousCargo.Cancel();

        // Creamos una nueva reserva válida para comprobar que el histórico
        // cancelado no impide cancelar la reserva actual.
        trip.ReserveCapacity(100m, 1m);

        var currentCargo = CreateReservedTripCargo(
            trip,
            request,
            100m,
            1m);

        db.TransportRequests.Update(request);
        db.Trips.Add(trip);
        db.TripCargos.AddRange(previousCargo, currentCargo);
        await db.SaveChangesAsync();

        var handler = new CancelTripCargoHandler(db, currentUserService);

        await handler.Handle(currentCargo.Id);

        Assert.Equal(
            TripCargoStatus.Cancelled,
            currentCargo.Status);

        Assert.Equal(
            900m,
            trip.AvailableWeightKg);

        Assert.Equal(
            4m,
            trip.AvailableVolumeM3);

        Assert.Equal(
            TransportRequestStatus.Published,
            request.Status);
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

    private static Trip CreateTrip(
        TestDbContext db,
        decimal availableWeightKg = 1000m,
        decimal availableVolumeM3 = 5m)
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
            availableWeightKg,
            availableVolumeM3);
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

    private static void SetRequestStatus(
        TransportRequest request,
        TransportRequestStatus status)
    {
        switch (status)
        {
            case TransportRequestStatus.Published:
                break;

            case TransportRequestStatus.Matching:
                request.StartMatching();
                break;

            case TransportRequestStatus.Accepted:
                request.AssignToTrip();
                break;

            case TransportRequestStatus.InProgress:
                request.AssignToTrip();
                request.Start();
                break;

            case TransportRequestStatus.Completed:
                request.AssignToTrip();
                request.Start();
                request.Complete();
                break;

            case TransportRequestStatus.Cancelled:
                request.Cancel();
                break;

            case TransportRequestStatus.Expired:
                request.Expire();
                break;

            case TransportRequestStatus.Draft:
                throw new InvalidOperationException(
                    "The test helper cannot move a published request back to draft.");
        }
    }
}



