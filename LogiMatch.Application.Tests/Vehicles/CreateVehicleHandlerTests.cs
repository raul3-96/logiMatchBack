using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.Vehicles;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMatch.Application.Tests.Vehicles;

public class CreateVehicleHandlerTests
{
    [Fact]
    public async Task Handle_WhenTransporterDoesNotExist_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateVehicleHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand()));

        Assert.Equal(
            "The specified transporter profile does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenLicensePlateAlreadyExists_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var transporter = CreateTransporter();
        var otherTransporter = CreateTransporter();
        var existingVehicle = CreateVehicle(
            otherTransporter.Id,
            licensePlate: "5678XYZ");

        db.TransporterProfiles.AddRange(transporter, otherTransporter);
        db.Vehicles.Add(existingVehicle);

        await db.SaveChangesAsync();

        var handler = new CreateVehicleHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(
                transporterProfileId: transporter.Id,
                licensePlate: " 5678xyz ")));

        Assert.Equal(
            "A vehicle with the specified license plate already exists.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenVehicleIsValid_ShouldCreateVehicle()
    {
        using var db = TestDbContextFactory.Create();

        var transporter = CreateTransporter();
        db.TransporterProfiles.Add(transporter);
        await db.SaveChangesAsync();

        var handler = new CreateVehicleHandler(db);

        var command = CreateCommand(
            transporterProfileId: transporter.Id,
            type: VehicleType.Van,
            brand: "  Mercedes-Benz  ",
            model: "  Sprinter  ",
            licensePlate: "  5678xyz  ",
            maxWeightKg: 1500m,
            maxVolumeM3: 10m,
            lengthM: 5.5m,
            widthM: 2m,
            heightM: 2.5m,
            hasTailLift: true,
            isRefrigerated: false);

        var vehicleId = await handler.Handle(command);

        var vehicle = await db.Vehicles
            .SingleAsync(x => x.Id == vehicleId);

        Assert.NotEqual(Guid.Empty, vehicleId);
        Assert.Equal(transporter.Id, vehicle.TransporterProfileId);
        Assert.Equal(VehicleType.Van, vehicle.Type);
        Assert.Equal("Mercedes-Benz", vehicle.Brand);
        Assert.Equal("Sprinter", vehicle.Model);
        Assert.Equal("5678XYZ", vehicle.LicensePlate);
        Assert.Equal(1500m, vehicle.MaxWeightKg);
        Assert.Equal(10m, vehicle.MaxVolumeM3);
        Assert.Equal(5.5m, vehicle.LengthM);
        Assert.Equal(2m, vehicle.WidthM);
        Assert.Equal(2.5m, vehicle.HeightM);
        Assert.True(vehicle.HasTailLift);
        Assert.False(vehicle.IsRefrigerated);
    }

    private static CreateVehicleCommand CreateCommand(
        Guid? transporterProfileId = null,
        VehicleType type = VehicleType.Van,
        string brand = "Mercedes-Benz",
        string model = "Sprinter",
        string licensePlate = "5678XYZ",
        decimal maxWeightKg = 1500m,
        decimal maxVolumeM3 = 10m,
        decimal lengthM = 5.5m,
        decimal widthM = 2m,
        decimal heightM = 2.5m,
        bool hasTailLift = true,
        bool isRefrigerated = false)
    {
        return new CreateVehicleCommand
        {
            TransporterProfileId = transporterProfileId ?? Guid.NewGuid(),
            Type = type,
            Brand = brand,
            Model = model,
            LicensePlate = licensePlate,
            MaxWeightKg = maxWeightKg,
            MaxVolumeM3 = maxVolumeM3,
            LengthM = lengthM,
            WidthM = widthM,
            HeightM = heightM,
            HasTailLift = hasTailLift,
            IsRefrigerated = isRefrigerated
        };
    }

    private static TransporterProfile CreateTransporter()
    {
        return new TransporterProfile(Guid.NewGuid());
    }

    private static Vehicle CreateVehicle(
        Guid transporterProfileId,
        string licensePlate = "5678XYZ")
    {
        return new Vehicle(
            transporterProfileId,
            VehicleType.Van,
            "Mercedes-Benz",
            "Sprinter",
            licensePlate,
            1500m,
            10m,
            5.5m,
            2m,
            2.5m,
            true,
            false);
    }
}