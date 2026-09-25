using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Application.Common.Services;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.Trips;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.Trips;

public class CompleteTripHandlerTests
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
    public async Task Handle_ShouldThrow_WhenTripIsPublished()
    {
        var db = await CreateValidDatabase();

        var trip = CreateTrip(db);
        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = CreateHandler(
            db,
            db.TransporterProfiles.Single().UserId);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(trip.Id));

        Assert.Equal(
            "Only in-progress trips can be completed.",
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
            "Only in-progress trips can be completed.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripIsCancelled()
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
            "Only in-progress trips can be completed.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripHasReservedCargo()
    {
        var db = await CreateValidDatabase();

        var request = CreateAcceptedRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        trip.Start();
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
            "The trip cannot be completed because it has active cargo.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTripHasInProgressCargo()
    {
        var db = await CreateValidDatabase();

        var request = CreateAcceptedRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        trip.Start();
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
            "The trip cannot be completed because it has active cargo.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldCompleteTrip_WhenThereAreNoCargos()
    {
        var db = await CreateValidDatabase();

        var trip = CreateTrip(db);
        trip.Start();

        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = CreateHandler(
            db,
            db.TransporterProfiles.Single().UserId);

        await handler.Handle(trip.Id);

        var savedTrip = db.Trips.Single();

        Assert.Equal(
            TripStatus.Completed,
            savedTrip.Status);
    }

    [Fact]
    public async Task Handle_ShouldCompleteTrip_WhenCargoIsCancelled()
    {
        var db = await CreateValidDatabase();

        var request = CreateAcceptedRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        trip.Start();
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

        Assert.Equal(
            TripStatus.Completed,
            savedTrip.Status);

        Assert.Equal(
            TripCargoStatus.Cancelled,
            savedCargo.Status);
    }

    [Fact]
    public async Task Handle_ShouldCompleteTrip_WhenCargoIsCompleted()
    {
        var db = await CreateValidDatabase();

        var request = CreateAcceptedRequest(db);
        db.TransportRequests.Add(request);

        var trip = CreateTrip(db);
        trip.Start();
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

        await handler.Handle(trip.Id);

        var savedTrip = db.Trips.Single();
        var savedCargo = db.TripCargos.Single();

        Assert.Equal(
            TripStatus.Completed,
            savedTrip.Status);

        Assert.Equal(
            TripCargoStatus.Completed,
            savedCargo.Status);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenCurrentUserDoesNotOwnTrip()
    {
        var db = await CreateValidDatabase();

        var trip = CreateTrip(db);
        trip.Start();
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

    private static CompleteTripHandler CreateHandler(
        IApplicationDbContext db,
        Guid currentUserId)
    {
        var currentUserService = new MockCurrentUserService(currentUserId);
        var accessService = new TripManagementAccessService(
            db,
            currentUserService);

        return new CompleteTripHandler(
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