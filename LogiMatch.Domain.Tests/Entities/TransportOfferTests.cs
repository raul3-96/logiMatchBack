using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Domain.Tests.Entities;

public class TransportOfferTests
{
    [Fact]
    public void Constructor_WhenValuesAreValid_ShouldCreatePendingOffer()
    {
        // Arrange
        var transportRequestId = Guid.NewGuid();
        var transporterProfileId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var pickupDate = DateTime.UtcNow.AddDays(1);
        var deliveryDate = DateTime.UtcNow.AddDays(2);

        // Act
        var offer = new TransportOffer(
            transportRequestId,
            transporterProfileId,
            vehicleId,
            350m,
            pickupDate,
            deliveryDate);

        // Assert
        Assert.NotEqual(Guid.Empty, offer.Id);
        Assert.Equal(transportRequestId, offer.TransportRequestId);
        Assert.Equal(transporterProfileId, offer.TransporterProfileId);
        Assert.Equal(vehicleId, offer.VehicleId);
        Assert.Equal(350m, offer.Price);
        Assert.Equal(pickupDate, offer.EstimatedPickupDate);
        Assert.Equal(deliveryDate, offer.EstimatedDeliveryDate);
        Assert.Equal(TransportOfferStatus.Pending, offer.Status);
        Assert.NotEqual(default, offer.CreatedAt);
    }

    [Fact]
    public void Accept_WhenOfferIsPending_ShouldChangeStatusToAccepted()
    {
        // Arrange
        var offer = CreateOffer();

        // Act
        offer.Accept();

        // Assert
        Assert.Equal(
            TransportOfferStatus.Accepted,
            offer.Status);
    }

    [Fact]
    public void Reject_WhenOfferIsPending_ShouldChangeStatusToRejected()
    {
        // Arrange
        var offer = CreateOffer();

        // Act
        offer.Reject();

        // Assert
        Assert.Equal(
            TransportOfferStatus.Rejected,
            offer.Status);
    }

    [Fact]
    public void Cancel_WhenOfferIsPending_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var offer = CreateOffer();

        // Act
        offer.Cancel();

        // Assert
        Assert.Equal(
            TransportOfferStatus.Cancelled,
            offer.Status);
    }

    [Fact]
    public void Accept_WhenOfferIsAccepted_ShouldThrow()
    {
        // Arrange
        var offer = CreateOffer();
        offer.Accept();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => offer.Accept());

        // Assert
        Assert.Equal(
            "Only pending offers can be accepted.",
            exception.Message);
    }

    [Fact]
    public void Accept_WhenOfferIsRejected_ShouldThrow()
    {
        // Arrange
        var offer = CreateOffer();
        offer.Reject();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => offer.Accept());

        // Assert
        Assert.Equal(
            "Only pending offers can be accepted.",
            exception.Message);
    }

    [Fact]
    public void Accept_WhenOfferIsCancelled_ShouldThrow()
    {
        // Arrange
        var offer = CreateOffer();
        offer.Cancel();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => offer.Accept());

        // Assert
        Assert.Equal(
            "Only pending offers can be accepted.",
            exception.Message);
    }

    [Fact]
    public void Reject_WhenOfferIsAccepted_ShouldThrow()
    {
        // Arrange
        var offer = CreateOffer();
        offer.Accept();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => offer.Reject());

        // Assert
        Assert.Equal(
            "Only pending offers can be rejected.",
            exception.Message);
    }

    [Fact]
    public void Reject_WhenOfferIsRejected_ShouldThrow()
    {
        // Arrange
        var offer = CreateOffer();
        offer.Reject();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => offer.Reject());

        // Assert
        Assert.Equal(
            "Only pending offers can be rejected.",
            exception.Message);
    }

    [Fact]
    public void Reject_WhenOfferIsCancelled_ShouldThrow()
    {
        // Arrange
        var offer = CreateOffer();
        offer.Cancel();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => offer.Reject());

        // Assert
        Assert.Equal(
            "Only pending offers can be rejected.",
            exception.Message);
    }

    [Fact]
    public void Cancel_WhenOfferIsAccepted_ShouldThrow()
    {
        // Arrange
        var offer = CreateOffer();
        offer.Accept();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => offer.Cancel());

        // Assert
        Assert.Equal(
            "Only pending offers can be cancelled.",
            exception.Message);
    }

    [Fact]
    public void Cancel_WhenOfferIsRejected_ShouldThrow()
    {
        // Arrange
        var offer = CreateOffer();
        offer.Reject();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => offer.Cancel());

        // Assert
        Assert.Equal(
            "Only pending offers can be cancelled.",
            exception.Message);
    }

    [Fact]
    public void Cancel_WhenOfferIsCancelled_ShouldThrow()
    {
        // Arrange
        var offer = CreateOffer();
        offer.Cancel();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => offer.Cancel());

        // Assert
        Assert.Equal(
            "Only pending offers can be cancelled.",
            exception.Message);
    }

    private static TransportOffer CreateOffer()
    {
        return new TransportOffer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            350m,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));
    }
}