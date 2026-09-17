using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Domain.Tests.Entities;

public class BookingTests
{
    [Fact]
    public void Constructor_WhenValuesAreValid_ShouldCreateConfirmedBooking()
    {
        // Arrange
        var transportRequestId = Guid.NewGuid();
        var transportOfferId = Guid.NewGuid();

        // Act
        var booking = new Booking(
            transportRequestId,
            transportOfferId);

        // Assert
        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(
            transportRequestId,
            booking.TransportRequestId);

        Assert.Equal(
            transportOfferId,
            booking.TransportOfferId);

        Assert.NotEqual(default, booking.CreatedAt);

        Assert.Equal(
            BookingStatus.Confirmed,
            booking.Status);
    }

    [Fact]
    public void Start_WhenBookingIsConfirmed_ShouldChangeStatusToInProgress()
    {
        // Arrange
        var booking = CreateBooking();

        // Act
        booking.Start();

        // Assert
        Assert.Equal(
            BookingStatus.InProgress,
            booking.Status);
    }

    [Fact]
    public void Complete_WhenBookingIsInProgress_ShouldChangeStatusToCompleted()
    {
        // Arrange
        var booking = CreateBooking();
        booking.Start();

        // Act
        booking.Complete();

        // Assert
        Assert.Equal(
            BookingStatus.Completed,
            booking.Status);
    }

    [Fact]
    public void Cancel_WhenBookingIsConfirmed_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var booking = CreateBooking();

        // Act
        booking.Cancel();

        // Assert
        Assert.Equal(
            BookingStatus.Cancelled,
            booking.Status);
    }

    [Fact]
    public void Start_WhenBookingIsInProgress_ShouldThrow()
    {
        // Arrange
        var booking = CreateBooking();
        booking.Start();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => booking.Start());

        // Assert
        Assert.Equal(
            "Only confirmed bookings can be started.",
            exception.Message);
    }

    [Fact]
    public void Start_WhenBookingIsCompleted_ShouldThrow()
    {
        // Arrange
        var booking = CreateBooking();
        booking.Start();
        booking.Complete();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => booking.Start());

        // Assert
        Assert.Equal(
            "Only confirmed bookings can be started.",
            exception.Message);
    }

    [Fact]
    public void Start_WhenBookingIsCancelled_ShouldThrow()
    {
        // Arrange
        var booking = CreateBooking();
        booking.Cancel();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => booking.Start());

        // Assert
        Assert.Equal(
            "Only confirmed bookings can be started.",
            exception.Message);
    }

    [Fact]
    public void Complete_WhenBookingIsConfirmed_ShouldThrow()
    {
        // Arrange
        var booking = CreateBooking();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => booking.Complete());

        // Assert
        Assert.Equal(
            "Only in-progress bookings can be completed.",
            exception.Message);
    }

    [Fact]
    public void Complete_WhenBookingIsCompleted_ShouldThrow()
    {
        // Arrange
        var booking = CreateBooking();
        booking.Start();
        booking.Complete();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => booking.Complete());

        // Assert
        Assert.Equal(
            "Only in-progress bookings can be completed.",
            exception.Message);
    }

    [Fact]
    public void Complete_WhenBookingIsCancelled_ShouldThrow()
    {
        // Arrange
        var booking = CreateBooking();
        booking.Cancel();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => booking.Complete());

        // Assert
        Assert.Equal(
            "Only in-progress bookings can be completed.",
            exception.Message);
    }

    [Fact]
    public void Cancel_WhenBookingIsInProgress_ShouldThrow()
    {
        // Arrange
        var booking = CreateBooking();
        booking.Start();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => booking.Cancel());

        // Assert
        Assert.Equal(
            "Only confirmed bookings can be cancelled.",
            exception.Message);
    }

    [Fact]
    public void Cancel_WhenBookingIsCompleted_ShouldThrow()
    {
        // Arrange
        var booking = CreateBooking();
        booking.Start();
        booking.Complete();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => booking.Cancel());

        // Assert
        Assert.Equal(
            "Only confirmed bookings can be cancelled.",
            exception.Message);
    }

    [Fact]
    public void Cancel_WhenBookingIsCancelled_ShouldThrow()
    {
        // Arrange
        var booking = CreateBooking();
        booking.Cancel();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => booking.Cancel());

        // Assert
        Assert.Equal(
            "Only confirmed bookings can be cancelled.",
            exception.Message);
    }

    private static Booking CreateBooking()
    {
        return new Booking(
            Guid.NewGuid(),
            Guid.NewGuid());
    }
}