using LogiMatch.Domain.Entities;
using Xunit;

namespace LogiMatch.Domain.Tests.Entities;

public class VehicleAvailabilityTests
{
    [Fact]
    public void Constructor_WhenDatesAreValid_ShouldCreateAvailability()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var availableFrom = new DateTime(2026, 9, 15, 8, 0, 0);
        var availableTo = new DateTime(2026, 9, 15, 18, 0, 0);

        // Act
        var availability = new VehicleAvailability(
            vehicleId,
            availableFrom,
            availableTo);

        // Assert
        Assert.NotEqual(Guid.Empty, availability.Id);
        Assert.Equal(vehicleId, availability.VehicleId);
        Assert.Equal(availableFrom, availability.AvailableFrom);
        Assert.Equal(availableTo, availability.AvailableTo);
    }

    [Fact]
    public void Constructor_WhenAvailableToEqualsAvailableFrom_ShouldThrow()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var date = new DateTime(2026, 9, 15, 8, 0, 0);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new VehicleAvailability(
                vehicleId,
                date,
                date));

        // Assert
        Assert.Equal(
            "AvailableTo must be after AvailableFrom.",
            exception.Message);
    }

    [Fact]
    public void Constructor_WhenAvailableToIsBeforeAvailableFrom_ShouldThrow()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var availableFrom = new DateTime(2026, 9, 15, 18, 0, 0);
        var availableTo = new DateTime(2026, 9, 15, 8, 0, 0);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new VehicleAvailability(
                vehicleId,
                availableFrom,
                availableTo));

        // Assert
        Assert.Equal(
            "AvailableTo must be after AvailableFrom.",
            exception.Message);
    }
}