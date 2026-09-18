using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Domain.Tests.Entities;

public class VehicleTests
{
    [Fact]
    public void Constructor_WhenValuesAreValid_ShouldCreateVehicle()
    {
        // Arrange
        var transporterProfileId = Guid.NewGuid();

        // Act
        var vehicle = new Vehicle(
            transporterProfileId,
            VehicleType.Van,
            "Mercedes-Benz",
            "Sprinter",
            "5678XYZ",
            1500m,
            10m,
            5.5m,
            2m,
            2.5m,
            true,
            false);

        // Assert
        Assert.Equal(transporterProfileId, vehicle.TransporterProfileId);
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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenMaxWeightIsInvalid_ShouldThrow(decimal maxWeightKg)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new Vehicle(
                Guid.NewGuid(),
                VehicleType.Van,
                "Mercedes-Benz",
                "Sprinter",
                "5678XYZ",
                maxWeightKg,
                10m,
                5.5m,
                2m,
                2.5m,
                true,
                false));

        Assert.Equal(
            "Maximum weight must be greater than zero.",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenMaxVolumeIsInvalid_ShouldThrow(decimal maxVolumeM3)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new Vehicle(
                Guid.NewGuid(),
                VehicleType.Van,
                "Mercedes-Benz",
                "Sprinter",
                "5678XYZ",
                1500m,
                maxVolumeM3,
                5.5m,
                2m,
                2.5m,
                true,
                false));

        Assert.Equal(
            "Maximum volume must be greater than zero.",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenLengthIsInvalid_ShouldThrow(decimal lengthM)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new Vehicle(
                Guid.NewGuid(),
                VehicleType.Van,
                "Mercedes-Benz",
                "Sprinter",
                "5678XYZ",
                1500m,
                10m,
                lengthM,
                2m,
                2.5m,
                true,
                false));

        Assert.Equal(
            "Vehicle length must be greater than zero.",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenWidthIsInvalid_ShouldThrow(decimal widthM)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new Vehicle(
                Guid.NewGuid(),
                VehicleType.Van,
                "Mercedes-Benz",
                "Sprinter",
                "5678XYZ",
                1500m,
                10m,
                5.5m,
                widthM,
                2.5m,
                true,
                false));

        Assert.Equal(
            "Vehicle width must be greater than zero.",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenHeightIsInvalid_ShouldThrow(decimal heightM)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new Vehicle(
                Guid.NewGuid(),
                VehicleType.Van,
                "Mercedes-Benz",
                "Sprinter",
                "5678XYZ",
                1500m,
                10m,
                5.5m,
                2m,
                heightM,
                true,
                false));

        Assert.Equal(
            "Vehicle height must be greater than zero.",
            exception.Message);
    }

    [Fact]
    public void Constructor_WhenTransporterProfileIdIsEmpty_ShouldThrow()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new Vehicle(
                Guid.Empty,
                VehicleType.Van,
                "Mercedes-Benz",
                "Sprinter",
                "5678XYZ",
                1500m,
                10m,
                5.5m,
                2m,
                2.5m,
                true,
                false));

        Assert.Equal(
            "Transporter profile ID cannot be empty.",
            exception.Message);
    }

    [Fact]
    public void Constructor_WhenVehicleTypeIsInvalid_ShouldThrow()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new Vehicle(
                Guid.NewGuid(),
                (VehicleType)999,
                "Mercedes-Benz",
                "Sprinter",
                "5678XYZ",
                1500m,
                10m,
                5.5m,
                2m,
                2.5m,
                true,
                false));

        Assert.Equal(
            "The specified vehicle type is not valid.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenBrandIsEmpty_ShouldThrow(string brand)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new Vehicle(
                Guid.NewGuid(),
                VehicleType.Van,
                brand,
                "Sprinter",
                "5678XYZ",
                1500m,
                10m,
                5.5m,
                2m,
                2.5m,
                true,
                false));

        Assert.Equal(
            "Vehicle brand cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenModelIsEmpty_ShouldThrow(string model)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new Vehicle(
                Guid.NewGuid(),
                VehicleType.Van,
                "Mercedes-Benz",
                model,
                "5678XYZ",
                1500m,
                10m,
                5.5m,
                2m,
                2.5m,
                true,
                false));

        Assert.Equal(
            "Vehicle model cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenLicensePlateIsEmpty_ShouldThrow(string licensePlate)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new Vehicle(
                Guid.NewGuid(),
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
                false));

        Assert.Equal(
            "Vehicle license plate cannot be empty.",
            exception.Message);
    }
}