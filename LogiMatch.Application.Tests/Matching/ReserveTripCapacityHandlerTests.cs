using LogiMatch.Application.Matching;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.Matching;

public class ReserveTripCapacityHandlerTests
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenTripDoesNotExist()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);

        var handler = new ReserveTripCapacityHandler(db);

        var command = CreateCommand(
            Guid.NewGuid(),
            request.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The specified trip does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripIsNotPublished()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);

        var trip = CreateTrip(db);
        trip.Start();

        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                CreateCommand(trip.Id, request.Id)));

        Assert.Equal(
            "Only published trips can reserve capacity.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenRequestDoesNotExist()
    {
        var db = await CreateValidDatabase();

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                CreateCommand(trip.Id, Guid.NewGuid())));

        Assert.Equal(
            "The specified transport request does not exist.",
            exception.Message);
    }

    [Theory]
    [InlineData(TransportRequestStatus.Accepted)]
    [InlineData(TransportRequestStatus.InProgress)]
    [InlineData(TransportRequestStatus.Completed)]
    [InlineData(TransportRequestStatus.Cancelled)]
    [InlineData(TransportRequestStatus.Expired)]
    public async Task Handle_ShouldThrow_WhenRequestIsNotAvailableForMatching(
        TransportRequestStatus status)
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);
        SetRequestStatus(request, status);

        var trip = CreateTrip(db);

        db.TransportRequests.Update(request);
        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                CreateCommand(trip.Id, request.Id)));

        Assert.Equal(
            "Only published or matching transport requests can be assigned to a trip.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenRequestIsAlreadyReserved()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);

        var trip1 = CreateTrip(db);
        var trip2 = CreateTrip(db);

        db.Trips.AddRange(trip1, trip2);

        var existingCargo = new TripCargo(
            trip1.Id,
            request.Id,
            100m,
            1m);

        db.TripCargos.Add(existingCargo);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                CreateCommand(trip2.Id, request.Id)));

        Assert.Equal(
            "The transport request is already reserved on another trip.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldAllowReservation_WhenPreviousReservationWasCancelled()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);

        var trip1 = CreateTrip(db);
        var trip2 = CreateTrip(db);

        db.Trips.AddRange(trip1, trip2);

        var existingCargo = new TripCargo(
            trip1.Id,
            request.Id,
            100m,
            1m);

        existingCargo.Cancel();

        db.TripCargos.Add(existingCargo);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var tripCargoId = await handler.Handle(
            CreateCommand(trip2.Id, request.Id));

        var newCargo = db.TripCargos
            .Single(x => x.Id == tripCargoId);

        Assert.Equal(trip2.Id, newCargo.TripId);
        Assert.Equal(request.Id, newCargo.TransportRequestId);
        Assert.Equal(TripCargoStatus.Reserved, newCargo.Status);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenRequestHasNoCargo()
    {
        var db = await CreateValidDatabase();

        var request = CreateRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                CreateCommand(trip.Id, request.Id)));

        Assert.Equal(
            "The transport request has no cargo.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenVehicleDoesNotExist()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);

        var trip = CreateTrip(
            db,
            vehicleId: Guid.NewGuid());

        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                CreateCommand(trip.Id, request.Id)));

        Assert.Equal(
            "The vehicle associated with the trip does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenVehicleDoesNotHaveRefrigeration()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(
            db,
            requiresRefrigeration: true);

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                CreateCommand(trip.Id, request.Id)));

        Assert.Equal(
            "The vehicle does not meet the refrigeration requirement.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenVehicleDoesNotHaveTailLift()
    {
        var db = await CreateValidDatabase(
            hasTailLift: false);

        var request = await CreateValidRequest(
            db,
            requiresTailLift: true);

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                CreateCommand(trip.Id, request.Id)));

        Assert.Equal(
            "The vehicle does not meet the tail lift requirement.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOriginDoesNotMatch()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);

        var trip = CreateTrip(
            db,
            originLocationId: Guid.NewGuid());

        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                CreateCommand(trip.Id, request.Id)));

        Assert.Equal(
            "The trip origin does not match the transport request pickup location.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenDestinationDoesNotMatch()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);

        var trip = CreateTrip(
            db,
            destinationLocationId: Guid.NewGuid());

        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                CreateCommand(trip.Id, request.Id)));

        Assert.Equal(
            "The trip destination does not match the transport request delivery location.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripDepartureIsAfterPickup()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(
            db,
            pickupDate: DateTime.UtcNow.AddDays(2));

        var trip = CreateTrip(
            db,
            departureDate: DateTime.UtcNow.AddDays(3),
            estimatedArrivalDate: DateTime.UtcNow.AddDays(3).AddHours(8));

        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                CreateCommand(trip.Id, request.Id)));

        Assert.Equal(
            "The trip departure date is after the requested pickup date.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripArrivesBeforePickup()
    {
        var db = await CreateValidDatabase();

        var pickup = DateTime.UtcNow.AddDays(2);

        var request = await CreateValidRequest(
            db,
            pickupDate: pickup);

        var trip = CreateTrip(
            db,
            departureDate: DateTime.UtcNow.AddDays(1),
            estimatedArrivalDate: DateTime.UtcNow.AddDays(1).AddHours(8));

        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                CreateCommand(trip.Id, request.Id)));

        Assert.Equal(
            "The trip arrives before the requested pickup date.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripArrivalIsAfterDeliveryDeadline()
    {
        var db = await CreateValidDatabase();

        var pickup = DateTime.UtcNow.AddDays(2);
        var deadline = pickup.AddHours(6);

        var request = await CreateValidRequest(
            db,
            pickupDate: pickup,
            deliveryDate: deadline);

        var trip = CreateTrip(
            db,
            departureDate: pickup.AddHours(-2),
            estimatedArrivalDate: pickup.AddHours(8));

        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                CreateCommand(trip.Id, request.Id)));

        Assert.Equal(
            "The trip arrival date is after the requested delivery deadline.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenWeightCapacityIsInsufficient()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(
            db,
            weightKg: 1200m);

        var trip = CreateTrip(
            db,
            availableWeightKg: 1000m);

        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                CreateCommand(trip.Id, request.Id)));

        Assert.Equal(
            "The trip does not have enough available weight.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenVolumeCapacityIsInsufficient()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(
            db,
            volumeM3: 8m);

        var trip = CreateTrip(
            db,
            availableVolumeM3: 5m);

        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                CreateCommand(trip.Id, request.Id)));

        Assert.Equal(
            "The trip does not have enough available volume.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenRequestAlreadyExistsOnSameTrip()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(db);

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        var existingCargo = new TripCargo(
            trip.Id,
            request.Id,
            100m,
            1m);

        db.TripCargos.Add(existingCargo);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                CreateCommand(trip.Id, request.Id)));

        Assert.Equal(
            "The transport request is already reserved on another trip.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldReserveCapacityCreateTripCargoAndAssignRequest()
    {
        var db = await CreateValidDatabase();

        var request = await CreateValidRequest(
            db,
            weightKg: 850m,
            volumeM3: 4.5m);

        var trip = CreateTrip(
            db,
            availableWeightKg: 1500m,
            availableVolumeM3: 10m);

        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        var tripCargoId = await handler.Handle(
            CreateCommand(trip.Id, request.Id));

        var savedTrip = db.Trips.Single();
        var savedCargo = db.TripCargos.Single();
        var savedRequest = db.TransportRequests.Single();

        Assert.Equal(tripCargoId, savedCargo.Id);

        Assert.Equal(
            650m,
            savedTrip.AvailableWeightKg);

        Assert.Equal(
            5.5m,
            savedTrip.AvailableVolumeM3);

        Assert.Equal(
            trip.Id,
            savedCargo.TripId);

        Assert.Equal(
            request.Id,
            savedCargo.TransportRequestId);

        Assert.Equal(
            850m,
            savedCargo.WeightKg);

        Assert.Equal(
            4.5m,
            savedCargo.VolumeM3);

        Assert.Equal(
            TripCargoStatus.Reserved,
            savedCargo.Status);

        Assert.Equal(
            TransportRequestStatus.Accepted,
            savedRequest.Status);

        Assert.Equal(
            FulfillmentMode.Trip,
            savedRequest.Fulfillment);
    }

    [Fact]
    public async Task Handle_ShouldReserveMultipleRequestsOnSameTrip()
    {
        var db = await CreateValidDatabase();

        var request1 = await CreateValidRequest(
            db,
            weightKg: 300m,
            volumeM3: 2m);

        var request2 = await CreateValidRequest(
            db,
            weightKg: 200m,
            volumeM3: 1m);

        var trip = CreateTrip(
            db,
            availableWeightKg: 1000m,
            availableVolumeM3: 5m);

        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new ReserveTripCapacityHandler(db);

        await handler.Handle(
            CreateCommand(trip.Id, request1.Id));

        await handler.Handle(
            CreateCommand(trip.Id, request2.Id));

        var savedTrip = db.Trips.Single();
        var cargos = db.TripCargos.ToList();

        Assert.Equal(500m, savedTrip.AvailableWeightKg);
        Assert.Equal(2m, savedTrip.AvailableVolumeM3);

        Assert.Equal(2, cargos.Count);

        Assert.All(
            cargos,
            cargo => Assert.Equal(
                TripCargoStatus.Reserved,
                cargo.Status));
    }

    private static ReserveTripCapacityCommand CreateCommand(
        Guid tripId,
        Guid requestId)
    {
        return new ReserveTripCapacityCommand
        {
            TripId = tripId,
            TransportRequestId = requestId
        };
    }

    private static async Task<TestDbContext> CreateValidDatabase(
        bool hasTailLift = true)
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
            hasTailLift,
            false);

        var origin = CreateLocation();
        var destination = CreateLocation();

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
        TestDbContext db,
        decimal weightKg = 100m,
        decimal volumeM3 = 1m,
        bool requiresRefrigeration = false,
        bool requiresTailLift = false,
        DateTime? pickupDate = null,
        DateTime? deliveryDate = null)
    {
        var locations = db.Locations.ToList();

        var request = new TransportRequest(
            Guid.NewGuid(),
            locations[0].Id,
            locations[1].Id,
            pickupDate ?? DateTime.UtcNow.AddDays(1).AddHours(1),
            deliveryDate);

        var cargo = new Cargo(
            request.Id,
            "Test cargo",
            weightKg,
            volumeM3,
            1,
            requiresRefrigeration,
            requiresTailLift);

        request.Publish();

        db.TransportRequests.Add(request);
        db.Cargos.Add(cargo);

        await db.SaveChangesAsync();

        return request;
    }

    private static TransportRequest CreateRequest(TestDbContext db)
    {
        var locations = db.Locations.ToList();

        var request = new TransportRequest(
            Guid.NewGuid(),
            locations[0].Id,
            locations[1].Id,
            DateTime.UtcNow.AddDays(1),
            null);

        request.Publish();

        return request;
    }

    private static Trip CreateTrip(
        TestDbContext db,
        Guid? vehicleId = null,
        Guid? originLocationId = null,
        Guid? destinationLocationId = null,
        DateTime? departureDate = null,
        DateTime? estimatedArrivalDate = null,
        decimal availableWeightKg = 1000m,
        decimal availableVolumeM3 = 5m)
    {
        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();
        var locations = db.Locations.ToList();

        var departure =
            departureDate ??
            DateTime.UtcNow.AddDays(1);

        var arrival =
            estimatedArrivalDate ??
            departure.AddHours(8);

        return new Trip(
            transporter.Id,
            vehicleId ?? vehicle.Id,
            originLocationId ?? locations[0].Id,
            destinationLocationId ?? locations[1].Id,
            departure,
            arrival,
            availableWeightKg,
            availableVolumeM3);
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

    private static void SetRequestStatus(
        TransportRequest request,
        TransportRequestStatus status)
    {
        switch (status)
        {
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

            case TransportRequestStatus.Published:
                // CreateValidRequest() ya deja la request en Published.
                break;

            case TransportRequestStatus.Matching:
                request.StartMatching();
                break;

            case TransportRequestStatus.Draft:
                // No se puede volver de Published a Draft.
                throw new InvalidOperationException(
                    "The test helper cannot move a published request back to draft.");
        }
    }
}