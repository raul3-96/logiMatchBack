using LogiMatch.Domain.Entities;
using Xunit;

namespace LogiMatch.Domain.Tests.Entities;

public class CargoTests
{
    [Fact]
    public void Constructor_WhenValuesAreValid_ShouldCreateCargo()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        // Act
        var cargo = new Cargo(
            requestId,
            "Cajas de material",
            850m,
            4.5m,
            10,
            false,
            true);

        // Assert
        Assert.Equal(requestId, cargo.TransportRequestId);
        Assert.Equal("Cajas de material", cargo.Description);
        Assert.Equal(850m, cargo.WeightKg);
        Assert.Equal(4.5m, cargo.VolumeM3);
        Assert.Equal(10, cargo.Quantity);
        Assert.False(cargo.RequiresRefrigeration);
        Assert.True(cargo.RequiresTailLift);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenWeightIsInvalid_ShouldThrow(decimal weightKg)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new Cargo(
                Guid.NewGuid(),
                "Cajas",
                weightKg,
                4.5m,
                10,
                false,
                true));

        // Assert
        Assert.Equal(
            "Cargo weight must be greater than zero.",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenVolumeIsInvalid_ShouldThrow(decimal volumeM3)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new Cargo(
                Guid.NewGuid(),
                "Cajas",
                850m,
                volumeM3,
                10,
                false,
                true));

        // Assert
        Assert.Equal(
            "Cargo volume must be greater than zero.",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenQuantityIsInvalid_ShouldThrow(int quantity)
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new Cargo(
                Guid.NewGuid(),
                "Cajas",
                850m,
                4.5m,
                quantity,
                false,
                true));

        // Assert
        Assert.Equal(
            "Cargo quantity must be greater than zero.",
            exception.Message);
    }
}