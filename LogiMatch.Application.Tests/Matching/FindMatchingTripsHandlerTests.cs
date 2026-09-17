using LogiMatch.Application.Matching;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Testing.Platform.Requests;
using Xunit;

namespace LogiMatch.Application.Tests.Matching;

public class FindMatchingTripsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenTransportRequestDoesNotExist()
    {
        var db = TestDbContextFactory.Create();

        var handler = new FindMatchingTripsHandler(db);

        var command = new FindMatchingTripsCommand
        {
            TransportRequestId = Guid.NewGuid()
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The specified transport request does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenRequestHasNoCargo()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        var handler = new FindMatchingTripsHandler(db);

        var command = new FindMatchingTripsCommand
        {
            TransportRequestId = request.Id
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The transport request has no cargo.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldReturnMatchingTrip_WhenAllConditionsMatch()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(
            db,
            request,
            weightKg: 500m,
            volumeM3: 2m,
            requiresRefrigeration: false,
            requiresTailLift: false);

        var vehicle = CreateVehicle(
            db,
            isRefrigerated: false,
            hasTailLift: false);

        var trip = CreateMatchingTrip(
            db,
            request,
            vehicle,
            availableWeightKg: 1000m,
            availableVolumeM3: 5m);

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var command = new FindMatchingTripsCommand
        {
            TransportRequestId = request.Id
        };

        var result = await handler.Handle(command);

        var matches = GetMatches(result);

        Assert.Single(matches);

        Assert.Equal(trip.Id, GetProperty<Guid>(matches[0], "TripId"));
    }

    [Fact]
    public async Task Handle_ShouldReturnRequiredCargoTotals()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 300m, 1.5m);
        AddCargo(db, request, 200m, 2.5m);

        var vehicle = CreateVehicle(db);
        CreateMatchingTrip(db, request, vehicle);

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var result = await handler.Handle(
            new FindMatchingTripsCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Equal(500m, GetProperty<decimal>(result, "RequiredWeightKg"));
        Assert.Equal(4m, GetProperty<decimal>(result, "RequiredVolumeM3"));
    }

    [Fact]
    public async Task Handle_ShouldReturnRequirementFlags()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(
            db,
            request,
            500m,
            2m,
            requiresRefrigeration: true,
            requiresTailLift: true);

        var vehicle = CreateVehicle(
            db,
            isRefrigerated: true,
            hasTailLift: true);

        CreateMatchingTrip(db, request, vehicle);

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var result = await handler.Handle(
            new FindMatchingTripsCommand
            {
                TransportRequestId = request.Id
            });

        Assert.True(GetProperty<bool>(result, "RequiresRefrigeration"));
        Assert.True(GetProperty<bool>(result, "RequiresTailLift"));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenTripIsNotPublished()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 2m);

        var vehicle = CreateVehicle(db);

        var trip = CreateMatchingTrip(db, request, vehicle);
        trip.Start();

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var result = await handler.Handle(
            new FindMatchingTripsCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenOriginDoesNotMatch()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(
            db,
            request,
            500m,
            2m);

        var vehicle = CreateVehicle(db);

        var trip = new Trip(
            vehicle.TransporterProfileId,
            vehicle.Id,
            Guid.NewGuid(), // origen DIFERENTE
            request.DeliveryLocationId,
            request.PickupDate.AddHours(-1),
            request.PickupDate.AddHours(2),
            1000m,
            5m);

        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var result = await handler.Handle(
            new FindMatchingTripsCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenDestinationDoesNotMatch()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 2m);

        var vehicle = CreateVehicle(db);

        CreateTripWithDifferentDestination(db, request, vehicle);

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var result = await handler.Handle(
            new FindMatchingTripsCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenTripDepartsAfterPickup()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 2m);

        var vehicle = CreateVehicle(db);

        var trip = new Trip(
            vehicle.TransporterProfileId,
            vehicle.Id,
            request.PickupLocationId,
            request.DeliveryLocationId,
            request.PickupDate.AddMinutes(1), // sale después del pickup
            request.PickupDate.AddHours(2),
            1000m,
            5m);

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var result = await handler.Handle(
            new FindMatchingTripsCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenTripArrivesBeforePickup()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 2m);

        var vehicle = CreateVehicle(db);

        var trip = new Trip(
           vehicle.TransporterProfileId,
           vehicle.Id,
           request.PickupLocationId,
           request.DeliveryLocationId,
           request.PickupDate.AddHours(-2),
           request.PickupDate.AddMinutes(-1), // llega antes del pickup
           1000m,
           5m);

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var result = await handler.Handle(
            new FindMatchingTripsCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenArrivalIsAfterDeliveryDeadline()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 2m);

        var vehicle = CreateVehicle(db);

        var trip = new Trip(
            vehicle.TransporterProfileId,
            vehicle.Id,
            request.PickupLocationId,
            request.DeliveryLocationId,
            request.PickupDate.AddHours(-1),
            request.DeliveryDate!.Value.AddMinutes(1),
            1000m,
            5m);

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var result = await handler.Handle(
            new FindMatchingTripsCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldMatch_WhenRequestHasNoDeliveryDeadline()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequestWithoutDeliveryDate(db);

        AddCargo(
            db,
            request,
            500m,
            2m);

        var vehicle = CreateVehicle(db);

        var trip = new Trip(
            vehicle.TransporterProfileId,
            vehicle.Id,
            request.PickupLocationId,
            request.DeliveryLocationId,
            request.PickupDate.AddHours(-1),
            request.PickupDate.AddDays(10),
            1000m,
            5m);

        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var result = await handler.Handle(
            new FindMatchingTripsCommand
            {
                TransportRequestId = request.Id
            });

        var matches = GetMatches(result);

        Assert.Single(matches);
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenWeightIsInsufficient()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 1001m, 2m);

        var vehicle = CreateVehicle(db);

        CreateMatchingTrip(
            db,
            request,
            vehicle,
            availableWeightKg: 1000m,
            availableVolumeM3: 5m);

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var result = await handler.Handle(
            new FindMatchingTripsCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenVolumeIsInsufficient()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 5.1m);

        var vehicle = CreateVehicle(db);

        CreateMatchingTrip(
            db,
            request,
            vehicle,
            availableWeightKg: 1000m,
            availableVolumeM3: 5m);

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var result = await handler.Handle(
            new FindMatchingTripsCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenRefrigerationIsRequired()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(
            db,
            request,
            500m,
            2m,
            requiresRefrigeration: true);

        var vehicle = CreateVehicle(
            db,
            isRefrigerated: false);

        CreateMatchingTrip(db, request, vehicle);

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var result = await handler.Handle(
            new FindMatchingTripsCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenTailLiftIsRequired()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(
            db,
            request,
            500m,
            2m,
            requiresTailLift: true);

        var vehicle = CreateVehicle(
            db,
            hasTailLift: false);

        CreateMatchingTrip(db, request, vehicle);

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var result = await handler.Handle(
            new FindMatchingTripsCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldNotMatch_WhenVehicleDoesNotExist()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 2m);

        var trip = CreateTrip(db ,Guid.NewGuid());

        // VehicleId apunta a uno inexistente.

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var result = await handler.Handle(
            new FindMatchingTripsCommand
            {
                TransportRequestId = request.Id
            });

        Assert.Empty(GetMatches(result));
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyMatchingTrips()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 500m, 2m);

        var matchingVehicle = CreateVehicle(db);
        var nonMatchingVehicle = CreateVehicle(db);

        var matchingTrip =
            CreateMatchingTrip(db, request, matchingVehicle);

        var nonMatchingTrip =
            CreateMatchingTrip(
                db,
                request,
                nonMatchingVehicle,
                availableWeightKg: 400m,
                availableVolumeM3: 5m);

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var result = await handler.Handle(
            new FindMatchingTripsCommand
            {
                TransportRequestId = request.Id
            });

        var matches = GetMatches(result);

        Assert.Single(matches);
        Assert.Equal(
            matchingTrip.Id,
            GetProperty<Guid>(matches[0], "TripId"));

        Assert.NotEqual(
            nonMatchingTrip.Id,
            GetProperty<Guid>(matches[0], "TripId"));
    }

    [Fact]
    public async Task Handle_ShouldCalculateRemainingCapacity()
    {
        var db = TestDbContextFactory.Create();

        var request = await CreateValidRequest(db);

        AddCargo(db, request, 400m, 1.5m);

        var vehicle = CreateVehicle(db);

        var trip = CreateMatchingTrip(
            db,
            request,
            vehicle,
            availableWeightKg: 1000m,
            availableVolumeM3: 5m);

        await db.SaveChangesAsync();

        var handler = new FindMatchingTripsHandler(db);

        var result = await handler.Handle(
            new FindMatchingTripsCommand
            {
                TransportRequestId = request.Id
            });

        var matches = GetMatches(result);

        Assert.Single(matches);

        Assert.Equal(
            600m,
            GetProperty<decimal>(
                matches[0],
                "RemainingWeightKg"));

        Assert.Equal(
            3.5m,
            GetProperty<decimal>(
                matches[0],
                "RemainingVolumeM3"));
    }

    // ---------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------

    private static async Task<TransportRequest> CreateValidRequest(
        TestDbContext db)
    {
        var request = new TransportRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2),
            DateTime.UtcNow.AddHours(10));

        db.TransportRequests.Add(request);

        await db.SaveChangesAsync();

        return request;
    }

    private static async Task<TransportRequest> CreateValidRequestWithoutDeliveryDate(
        TestDbContext db)
    {
        var request = new TransportRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2),
            null);

        db.TransportRequests.Add(request);

        await db.SaveChangesAsync();

        return request;
    }

    private static void AddCargo(
        TestDbContext db,
        TransportRequest request,
        decimal weightKg,
        decimal volumeM3,
        bool requiresRefrigeration = false,
        bool requiresTailLift = false)
    {
        var cargo = new Cargo(
            request.Id,
            "Test cargo",
            weightKg,
            volumeM3,
            1,
            requiresRefrigeration,
            requiresTailLift);

        db.Cargos.Add(cargo);
    }

    private static Vehicle CreateVehicle(
        TestDbContext db,
        bool isRefrigerated = false,
        bool hasTailLift = false)
    {
        var vehicle = new Vehicle(
            Guid.NewGuid(),
            VehicleType.Van,
            "Mercedes",
            "Sprinter",
            $"TEST-{Guid.NewGuid():N}".Substring(0, 10),
            1500m,
            10m,
            6m,
            2m,
            2.5m,
            hasTailLift,
            isRefrigerated);

        db.Vehicles.Add(vehicle);

        return vehicle;
    }

    private static Trip CreateMatchingTrip(
        TestDbContext db,
        TransportRequest request,
        Vehicle vehicle,
        decimal availableWeightKg = 1000m,
        decimal availableVolumeM3 = 5m)
    {
        var trip = new Trip(
            vehicle.TransporterProfileId,
            vehicle.Id,
            request.PickupLocationId,
            request.DeliveryLocationId,
            request.PickupDate.AddHours(-1),
            request.PickupDate.AddHours(2),
            availableWeightKg,
            availableVolumeM3);

        db.Trips.Add(trip);

        return trip;
    }

    private static Trip CreateTrip(TestDbContext db, Guid vehicle)
    {
        var trip = new Trip(
            Guid.NewGuid(),
            vehicle,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(5),
            1000m,
            5m);

        db.Trips.Add(trip);

        return trip;
    }

    private static Trip CreateTripWithDifferentDestination(
        TestDbContext db,
        TransportRequest request,
        Vehicle vehicle)
    {
        var trip = new Trip(
            vehicle.TransporterProfileId,
            vehicle.Id,
            request.PickupLocationId,
            Guid.NewGuid(),                  // destino diferente
            request.PickupDate.AddHours(-1),
            request.PickupDate.AddHours(2),
            1000m,
            5m);
        return trip;
    }

    private static object[] GetMatches(object result)
    {
        var property = result.GetType().GetProperty("Matches")!;
        var value = property.GetValue(result)!;

        return ((System.Collections.IEnumerable)value)
            .Cast<object>()
            .ToArray();
    }

    private static T GetProperty<T>(
        object obj,
        string propertyName)
    {
        var property = obj.GetType().GetProperty(propertyName)!;
        return (T)property.GetValue(obj)!;
    }
}