using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Application.Common.Services;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.Trips;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.Trips;

public class CancelTripHandlerTests
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenTripDoesNotExist()
    {
        var db = TestDbContextFactory.Create();
        var handler = CreateHandler(
            db,
            Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(Guid.NewGuid()));

        Assert.Equal(
            "The specified trip does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripIsInProgress()
    {
        var db = await CreateValidDatabase();

        var trip = CreateTrip(db);
        trip.Start();

        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var handler = CreateHandler(
            db,
            db.TransporterProfiles.Single().UserId);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(trip.Id));

        Assert.Equal(
            "Only published trips can be cancelled.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripIsCompleted()
    {
        var db = await CreateValidDatabase();

        var trip = CreateTrip(db);
        trip.Start();
        trip.Complete();

        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var handler = CreateHandler(
            db,
            db.TransporterProfiles.Single().UserId);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(trip.Id));

        Assert.Equal(
            "Only published trips can be cancelled.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripIsAlreadyCancelled()
    {
        var db = await CreateValidDatabase();

        var trip = CreateTrip(db);
        trip.Cancel();

        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var handler = CreateHandler(
            db,
            db.TransporterProfiles.Single().UserId);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(trip.Id));

        Assert.Equal(
            "Only published trips can be cancelled.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripHasCargoInProgress()
    {
        var db = await CreateValidDatabase();

        var request = CreateAcceptedRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        var cargo = new TripCargo(
            trip.Id,
            request.Id,
            100m,
            1m);

        cargo.Start();

        db.TripCargos.Add(cargo);
        await db.SaveChangesAsync();

        var handler = CreateHandler(
            db,
            db.TransporterProfiles.Single().UserId);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(trip.Id));

        Assert.Equal(
            "The trip cannot be cancelled because it has cargo in progress.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripHasCargoWithInvalidStatus()
    {
        var db = await CreateValidDatabase();

        var request = CreateAcceptedRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        var cargo = new TripCargo(
            trip.Id,
            request.Id,
            100m,
            1m);

        cargo.Start();
        cargo.Complete();

        db.TripCargos.Add(cargo);
        await db.SaveChangesAsync();

        var handler = CreateHandler(
            db,
            db.TransporterProfiles.Single().UserId);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(trip.Id));

        Assert.Equal(
            "The trip contains cargo with an invalid status for cancellation.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTransportRequestDoesNotExist()
    {
        var db = await CreateValidDatabase();

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        var cargo = new TripCargo(
            trip.Id,
            Guid.NewGuid(),
            100m,
            1m);

        db.TripCargos.Add(cargo);

        await db.SaveChangesAsync();

        var handler = CreateHandler(
            db,
            db.TransporterProfiles.Single().UserId);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(trip.Id));

        Assert.Equal(
            "The transport request associated with the trip cargo does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTransportRequestIsNotAccepted()
    {
        var db = await CreateValidDatabase();

        var request = CreatePublishedRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        var cargo = new TripCargo(
            trip.Id,
            request.Id,
            100m,
            1m);

        db.TripCargos.Add(cargo);

        await db.SaveChangesAsync();

        var handler = CreateHandler(
            db,
            db.TransporterProfiles.Single().UserId);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(trip.Id));

        Assert.Equal(
            "A reserved trip cargo must belong to an accepted transport request.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTransportRequestHasActiveBooking()
    {
        var db = await CreateValidDatabase();

        var request = CreateAcceptedRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        var cargo = new TripCargo(
            trip.Id,
            request.Id,
            100m,
            1m);

        db.TripCargos.Add(cargo);

        var offer = CreateOffer(
            request.Id,
            db.Vehicles.Single().Id);

        db.TransportOffers.Add(offer);

        var booking = new Booking(
            request.Id,
            offer.Id);

        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = CreateHandler(
            db,
            db.TransporterProfiles.Single().UserId);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(trip.Id));

        Assert.Equal(
            "The trip cannot be cancelled because one of its transport requests has an active booking.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldAllowCancellation_WhenBookingIsCancelled()
    {
        var db = await CreateValidDatabase();

        var request = CreateAcceptedRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        trip.ReserveCapacity(100m, 1m);
        db.Trips.Add(trip);

        var cargo = new TripCargo(
            trip.Id,
            request.Id,
            100m,
            1m);

        db.TripCargos.Add(cargo);

        var offer = CreateOffer(
            request.Id,
            db.Vehicles.Single().Id);

        db.TransportOffers.Add(offer);

        var booking = new Booking(
            request.Id,
            offer.Id);

        booking.Cancel();

        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = CreateHandler(
            db,
            db.TransporterProfiles.Single().UserId);

        await handler.Handle(trip.Id);

        var savedTrip = db.Trips.Single();
        var savedCargo = db.TripCargos.Single();
        var savedRequest = db.TransportRequests.Single();

        Assert.Equal(TripStatus.Cancelled, savedTrip.Status);
        Assert.Equal(TripCargoStatus.Cancelled, savedCargo.Status);
        Assert.Equal(
            TransportRequestStatus.Published,
            savedRequest.Status);
    }

    [Fact]
    public async Task Handle_ShouldCancelTrip_WhenThereAreNoCargos()
    {
        var db = await CreateValidDatabase();

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = CreateHandler(
            db,
            db.TransporterProfiles.Single().UserId);

        await handler.Handle(trip.Id);

        var savedTrip = db.Trips.Single();

        Assert.Equal(
            TripStatus.Cancelled,
            savedTrip.Status);
    }

    [Fact]
    public async Task Handle_ShouldIgnoreCancelledCargo()
    {
        var db = await CreateValidDatabase();

        var request = CreateAcceptedRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        var cargo = new TripCargo(
            trip.Id,
            request.Id,
            100m,
            1m);

        cargo.Cancel();

        db.TripCargos.Add(cargo);

        await db.SaveChangesAsync();

        var handler = CreateHandler(
            db,
            db.TransporterProfiles.Single().UserId);

        await handler.Handle(trip.Id);

        var savedTrip = db.Trips.Single();
        var savedCargo = db.TripCargos.Single();
        var savedRequest = db.TransportRequests.Single();

        Assert.Equal(
            TripStatus.Cancelled,
            savedTrip.Status);

        Assert.Equal(
            TripCargoStatus.Cancelled,
            savedCargo.Status);

        Assert.Equal(
            TransportRequestStatus.Accepted,
            savedRequest.Status);
    }

    [Fact]
    public async Task Handle_ShouldCancelTripAndReservedCargo()
    {
        var db = await CreateValidDatabase();

        var request = CreateAcceptedRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        trip.ReserveCapacity(100m, 1m);
        db.Trips.Add(trip);

        var cargo = new TripCargo(
            trip.Id,
            request.Id,
            100m,
            1m);

        db.TripCargos.Add(cargo);

        await db.SaveChangesAsync();

        var handler = CreateHandler(
            db,
            db.TransporterProfiles.Single().UserId);

        await handler.Handle(trip.Id);

        var savedTrip = db.Trips.Single();
        var savedCargo = db.TripCargos.Single();
        var savedRequest = db.TransportRequests.Single();

        Assert.Equal(
            TripStatus.Cancelled,
            savedTrip.Status);

        Assert.Equal(
            TripCargoStatus.Cancelled,
            savedCargo.Status);

        Assert.Equal(
            TransportRequestStatus.Published,
            savedRequest.Status);
    }

    [Fact]
    public async Task Handle_ShouldReleaseCargoCapacity()
    {
        var db = await CreateValidDatabase();

        var request = CreateAcceptedRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        trip.ReserveCapacity(100m, 1m);

        var cargo = new TripCargo(
            trip.Id,
            request.Id,
            100m,
            1m);

        db.TripCargos.Add(cargo);

        await db.SaveChangesAsync();

        Assert.Equal(900m, trip.AvailableWeightKg);
        Assert.Equal(4m, trip.AvailableVolumeM3);

        var handler = CreateHandler(
            db,
            db.TransporterProfiles.Single().UserId);

        await handler.Handle(trip.Id);

        var savedTrip = db.Trips.Single();

        Assert.Equal(
            1000m,
            savedTrip.AvailableWeightKg);

        Assert.Equal(
            5m,
            savedTrip.AvailableVolumeM3);
    }

    [Fact]
    public async Task Handle_ShouldCancelMultipleCargosAndReturnRequestsToPublished()
    {
        var db = await CreateValidDatabase();

        var request1 = CreateAcceptedRequest(db);
        var request2 = CreateAcceptedRequest(db);

        db.TransportRequests.AddRange(request1, request2);

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        trip.ReserveCapacity(300m, 3m);

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

        var handler = CreateHandler(
            db,
            db.TransporterProfiles.Single().UserId);

        await handler.Handle(trip.Id);

        var savedTrip = db.Trips.Single();
        var savedCargos = db.TripCargos.ToList();
        var savedRequests = db.TransportRequests.ToList();

        Assert.Equal(
            TripStatus.Cancelled,
            savedTrip.Status);

        Assert.Equal(
            1000m,
            savedTrip.AvailableWeightKg);

        Assert.Equal(
            5m,
            savedTrip.AvailableVolumeM3);

        Assert.All(
            savedCargos,
            cargo => Assert.Equal(
                TripCargoStatus.Cancelled,
                cargo.Status));

        Assert.All(
            savedRequests,
            request => Assert.Equal(
                TransportRequestStatus.Published,
                request.Status));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenCurrentUserDoesNotOwnTrip()
    {
        var db = await CreateValidDatabase();

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = CreateHandler(
            db,
            Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(trip.Id));

        Assert.Equal(
            "Only the company owner, an administrator, or the profile owner can manage this trip.",
            exception.Message);
    }

    private static CancelTripHandler CreateHandler(
        IApplicationDbContext db,
        Guid currentUserId)
    {
        var currentUserService = new MockCurrentUserService(currentUserId);
        var accessService = new TripManagementAccessService(
            db,
            currentUserService);

        return new CancelTripHandler(
            db,
            accessService);
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

        var vehicle = CreateVehicle(transporter.Id);
        var origin = CreateLocation();
        var destination = CreateLocation();

        var availability = new VehicleAvailability(
            vehicle.Id,
            DateTime.UtcNow.AddHours(-1),
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

    private static TransportRequest CreatePublishedRequest(TestDbContext db)
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

    private static TransportOffer CreateOffer(
        Guid requestId,
        Guid vehicleId)
    {
        return new TransportOffer(
            requestId,
            Guid.NewGuid(),
            vehicleId,
            350m,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(6));
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

    private sealed class MockCurrentUserService : ICurrentUserService
    {
        public MockCurrentUserService(Guid userId)
        {
            UserId = userId;
        }

        public Guid UserId { get; }
    }
}