using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Domain.Tests.Entities;

public class TripCargoTests
{
    [Fact]
    public void Start_WhenCargoIsReserved_ShouldChangeStatusToInProgress()
    {
        // Arrange
        var tripCargo = CreateTripCargo();

        // Act
        tripCargo.Start();

        // Assert
        Assert.Equal(
            TripCargoStatus.InProgress,
            tripCargo.Status);
    }

    [Fact]
    public void Complete_WhenCargoIsInProgress_ShouldChangeStatusToCompleted()
    {
        // Arrange
        var tripCargo = CreateTripCargo();
        tripCargo.Start();

        // Act
        tripCargo.Complete();

        // Assert
        Assert.Equal(
            TripCargoStatus.Completed,
            tripCargo.Status);
    }

    [Fact]
    public void Cancel_WhenCargoIsReserved_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var tripCargo = CreateTripCargo();

        // Act
        tripCargo.Cancel();

        // Assert
        Assert.Equal(
            TripCargoStatus.Cancelled,
            tripCargo.Status);
    }

    [Fact]
    public void Start_WhenCargoIsInProgress_ShouldThrow()
    {
        // Arrange
        var tripCargo = CreateTripCargo();
        tripCargo.Start();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => tripCargo.Start());

        // Assert
        Assert.Equal(
            "Only reserved trip cargo can be started.",
            exception.Message);
    }

    [Fact]
    public void Start_WhenCargoIsCompleted_ShouldThrow()
    {
        // Arrange
        var tripCargo = CreateTripCargo();
        tripCargo.Start();
        tripCargo.Complete();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => tripCargo.Start());

        // Assert
        Assert.Equal(
            "Only reserved trip cargo can be started.",
            exception.Message);
    }

    [Fact]
    public void Complete_WhenCargoIsReserved_ShouldThrow()
    {
        // Arrange
        var tripCargo = CreateTripCargo();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => tripCargo.Complete());

        // Assert
        Assert.Equal(
            "Only in-progress trip cargo can be completed.",
            exception.Message);
    }
    [Fact]
    public void Complete_WhenCargoIsCompleted_ShouldThrow()
    {
        // Arrange
        var tripCargo = CreateTripCargo();
        tripCargo.Start();
        tripCargo.Complete();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => tripCargo.Complete());

        // Assert
        Assert.Equal(
            "Only in-progress trip cargo can be completed.",
            exception.Message);
    }

    [Fact]
    public void Cancel_WhenCargoIsInProgress_ShouldThrow()
    {
        // Arrange
        var tripCargo = CreateTripCargo();
        tripCargo.Start();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => tripCargo.Cancel());

        // Assert
        Assert.Equal(
            "Only reserved trip cargo can be cancelled.",
            exception.Message);
    }

    [Fact]
    public void Cancel_WhenCargoIsCompleted_ShouldThrow()
    {
        // Arrange
        var tripCargo = CreateTripCargo();
        tripCargo.Start();
        tripCargo.Complete();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => tripCargo.Cancel());

        // Assert
        Assert.Equal(
            "Only reserved trip cargo can be cancelled.",
            exception.Message);
    }

    [Fact]
    public void Cancel_WhenCargoIsCancelled_ShouldThrow()
    {
        // Arrange
        var tripCargo = CreateTripCargo();
        tripCargo.Cancel();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => tripCargo.Cancel());

        // Assert
        Assert.Equal(
            "Only reserved trip cargo can be cancelled.",
            exception.Message);
    }


    private static TripCargo CreateTripCargo()
    {
        return new TripCargo(
            Guid.NewGuid(),
            Guid.NewGuid(),
            850m,
            4.5m);
    }
}
