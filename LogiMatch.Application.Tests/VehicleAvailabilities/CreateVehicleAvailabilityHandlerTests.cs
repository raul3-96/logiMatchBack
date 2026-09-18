using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.VehicleAvailabilities;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMatch.Application.Tests.VehicleAvailabilities;

public class CreateVehicleAvailabilityHandlerTests
{
    [Fact]
    public async Task Handle_WhenVehicleDoesNotExist_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateVehicleAvailabilityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand()));

        Assert.Equal(
            "The specified vehicle does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenAvailableToIsBeforeAvailableFrom_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var vehicle = CreateVehicle();
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        var handler = new CreateVehicleAvailabilityHandler(db);

        var from = DateTime.UtcNow.AddDays(1);
        var to = from.AddHours(-1);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(
                vehicleId: vehicle.Id,
                availableFrom: from,
                availableTo: to)));

        Assert.Equal(
            "AvailableTo must be after AvailableFrom.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenAvailableFromIsInThePast_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var vehicle = CreateVehicle();
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        var handler = new CreateVehicleAvailabilityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(
                vehicleId: vehicle.Id,
                availableFrom: DateTime.UtcNow.AddMinutes(-5),
                availableTo: DateTime.UtcNow.AddHours(2))));

        Assert.Equal(
            "AvailableFrom cannot be in the past.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenAvailabilityOverlaps_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var vehicle = CreateVehicle();
        var existingAvailability = new VehicleAvailability(
            vehicle.Id,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));

        db.Vehicles.Add(vehicle);
        db.VehicleAvailabilities.Add(existingAvailability);
        await db.SaveChangesAsync();

        var handler = new CreateVehicleAvailabilityHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(
                vehicleId: vehicle.Id,
                availableFrom: DateTime.UtcNow.AddDays(1).AddHours(1),
                availableTo: DateTime.UtcNow.AddDays(2).AddHours(1))));

        Assert.Equal(
            "The vehicle already has an availability that overlaps with the specified dates.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenAvailabilityIsValid_ShouldCreateAvailability()
    {
        using var db = TestDbContextFactory.Create();

        var vehicle = CreateVehicle();
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        var handler = new CreateVehicleAvailabilityHandler(db);

        var availableFrom = DateTime.UtcNow.AddDays(1);
        var availableTo = availableFrom.AddDays(2);

        var availabilityId = await handler.Handle(CreateCommand(
            vehicleId: vehicle.Id,
            availableFrom: availableFrom,
            availableTo: availableTo));

        var availability = await db.VehicleAvailabilities
            .SingleAsync(x => x.Id == availabilityId);

        Assert.NotEqual(Guid.Empty, availabilityId);
        Assert.Equal(vehicle.Id, availability.VehicleId);
        Assert.Equal(availableFrom, availability.AvailableFrom);
        Assert.Equal(availableTo, availability.AvailableTo);
    }

    private static CreateVehicleAvailabilityCommand CreateCommand(
        Guid? vehicleId = null,
        DateTime? availableFrom = null,
        DateTime? availableTo = null)
    {
        var from = availableFrom ?? DateTime.UtcNow.AddDays(1);
        var to = availableTo ?? from.AddDays(1);

        return new CreateVehicleAvailabilityCommand
        {
            VehicleId = vehicleId ?? Guid.NewGuid(),
            AvailableFrom = from,
            AvailableTo = to
        };
    }

    private static Vehicle CreateVehicle()
    {
        return new Vehicle(
            Guid.NewGuid(),
            VehicleType.Van,
            "Mercedes-Benz",
            "Sprinter",
            Guid.NewGuid().ToString()[..8].ToUpperInvariant(),
            1500m,
            10m,
            5.5m,
            2m,
            2.5m,
            true,
            false);
    }
}