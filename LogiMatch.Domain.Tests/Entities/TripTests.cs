using LogiMatch.Domain.Entities;
using Xunit;

namespace LogiMatch.Domain.Tests.Entities;

public class TripTests
{
    [Fact]
    public void ReserveCapacity_WhenCapacityIsAvailable_ShouldReduceAvailableCapacity()
    {
        // Arrange
        var trip = new Trip(
            transporterProfileId: Guid.NewGuid(),
            vehicleId: Guid.NewGuid(),
            originLocationId: Guid.NewGuid(),
            destinationLocationId: Guid.NewGuid(),
            departureDate: DateTime.UtcNow.AddHours(1),
            estimatedArrivalDate: DateTime.UtcNow.AddHours(5),
            availableWeightKg: 1000,
            availableVolumeM3: 10);

        // Act
        trip.ReserveCapacity(300, 4);

        // Assert
        Assert.Equal(700, trip.AvailableWeightKg);
        Assert.Equal(6, trip.AvailableVolumeM3);
    }

    [Fact]
    public void ReserveCapacity_WhenWeightIsInsufficient_ShouldThrow()
    {
        // Arrange
        var trip = new Trip(
            transporterProfileId: Guid.NewGuid(),
            vehicleId: Guid.NewGuid(),
            originLocationId: Guid.NewGuid(),
            destinationLocationId: Guid.NewGuid(),
            departureDate: DateTime.UtcNow.AddHours(1),
            estimatedArrivalDate: DateTime.UtcNow.AddHours(5),
            availableWeightKg: 500,
            availableVolumeM3: 10);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => trip.ReserveCapacity(600, 4));

        Assert.Equal(
            "The trip does not have enough available weight.",
            exception.Message);
    }

    [Fact]
    public void ReserveCapacity_WhenVolumeIsInsufficient_ShouldThrow()
    {
        // Arrange
        var trip = new Trip(
            transporterProfileId: Guid.NewGuid(),
            vehicleId: Guid.NewGuid(),
            originLocationId: Guid.NewGuid(),
            destinationLocationId: Guid.NewGuid(),
            departureDate: DateTime.UtcNow.AddHours(1),
            estimatedArrivalDate: DateTime.UtcNow.AddHours(5),
            availableWeightKg: 1000,
            availableVolumeM3: 5);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => trip.ReserveCapacity(600, 6));

        Assert.Equal(
            "The trip does not have enough available volume.",
            exception.Message);
    }

    [Fact]
    public void ReleaseCapacity_WhenCapacityWasReserved_ShouldRestoreAvailableCapacity()
    {
        // Arrange
        var trip = new Trip(
            transporterProfileId: Guid.NewGuid(),
            vehicleId: Guid.NewGuid(),
            originLocationId: Guid.NewGuid(),
            destinationLocationId: Guid.NewGuid(),
            departureDate: DateTime.UtcNow.AddHours(1),
            estimatedArrivalDate: DateTime.UtcNow.AddHours(5),
            availableWeightKg: 1000,
            availableVolumeM3: 10);

        trip.ReserveCapacity(300, 4);

        // Act
        trip.ReleaseCapacity(300, 4);

        // Assert
        Assert.Equal(1000, trip.AvailableWeightKg);
        Assert.Equal(10, trip.AvailableVolumeM3);
    }

    [Fact]
    public void ReleaseCapacity_WhenExceedsInitialWeight_ShouldThrow()
    {
        // Arrange
        var trip = new Trip(
            transporterProfileId: Guid.NewGuid(),
            vehicleId: Guid.NewGuid(),
            originLocationId: Guid.NewGuid(),
            destinationLocationId: Guid.NewGuid(),
            departureDate: DateTime.UtcNow.AddHours(1),
            estimatedArrivalDate: DateTime.UtcNow.AddHours(5),
            availableWeightKg: 1000,
            availableVolumeM3: 10);

        trip.ReserveCapacity(300, 4);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => trip.ReleaseCapacity(800, 4));

        Assert.Equal(
            "Released weight exceeds the trip's initial available capacity.",
            exception.Message);
    }

    [Fact]
    public void ReleaseCapacity_WhenExceedsInitialVolume_ShouldThrow()
    {
        // Arrange
        var trip = new Trip(
            transporterProfileId: Guid.NewGuid(),
            vehicleId: Guid.NewGuid(),
            originLocationId: Guid.NewGuid(),
            destinationLocationId: Guid.NewGuid(),
            departureDate: DateTime.UtcNow.AddHours(1),
            estimatedArrivalDate: DateTime.UtcNow.AddHours(5),
            availableWeightKg: 1000,
            availableVolumeM3: 10);

        trip.ReserveCapacity(300, 4);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => trip.ReleaseCapacity(300, 11));

        Assert.Equal(
            "Released volume exceeds the trip's initial available capacity.",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void ReserveCapacity_WhenWeightIsInvalid_ShouldThrow(decimal weight)
    {
        // Arrange
        var trip = new Trip(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(5),
            1000,
            10);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => trip.ReserveCapacity(weight, 2));

        Assert.Equal(
            "Weight must be greater than zero.",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReserveCapacity_WhenVolumeIsInvalid_ShouldThrow(decimal volume)
    {
        // Arrange
        var trip = new Trip(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(5),
            1000,
            10);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => trip.ReserveCapacity(100, volume));

        Assert.Equal(
            "Volume must be greater than zero.",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void ReleaseCapacity_WhenWeightIsInvalid_ShouldThrow(decimal weight)
    {
        // Arrange
        var trip = new Trip(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(5),
            1000,
            10);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => trip.ReleaseCapacity(weight, 2));

        Assert.Equal(
            "Weight to release must be greater than zero. (Parameter 'weightKg')",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReleaseCapacity_WhenVolumeIsInvalid_ShouldThrow(decimal volume)
    {
        // Arrange
        var trip = new Trip(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(5),
            1000,
            10);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => trip.ReleaseCapacity(100, volume));

        Assert.Equal(
            "Volume to release must be greater than zero. (Parameter 'volumeM3')",
            exception.Message);
    }
}