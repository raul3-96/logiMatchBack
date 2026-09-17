using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Domain.Tests.Entities;

public class TransportRequestTests
{
    [Fact]
    public void Publish_WhenRequestIsDraft_ShouldChangeStatusToPublished()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        request.Publish();

        // Assert
        Assert.Equal(TransportRequestStatus.Published, request.Status);
    }

    [Fact]
    public void StartMatching_WhenRequestIsDraft_ShouldThrow()
    {
        // Arrange
        var request = CreateRequest();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => request.StartMatching());

        Assert.Equal(
            "Only published requests can start matching.",
            exception.Message);
    }

    [Fact]
    public void StartMatching_WhenRequestIsPublished_ShouldChangeStatusToMatching()
    {
        // Arrange
        var request = CreateRequest();

        request.Publish();

        // Act
        request.StartMatching();

        // Assert
        Assert.Equal(TransportRequestStatus.Matching, request.Status);
    }

    [Fact]
    public void MarkOffersReceived_WhenRequestIsPublished_ShouldChangeStatusToOffersReceived()
    {
        // Arrange
        var request = CreateRequest();

        request.Publish();

        // Act
        request.MarkOffersReceived();

        // Assert
        Assert.Equal(TransportRequestStatus.OffersReceived, request.Status);
    }

    [Fact]
    public void MarkOffersReceived_WhenRequestIsMatching_ShouldChangeStatusToOffersReceived()
    {
        // Arrange
        var request = CreateRequest();

        request.Publish();
        request.StartMatching();

        // Act
        request.MarkOffersReceived();

        // Assert
        Assert.Equal(TransportRequestStatus.OffersReceived, request.Status);
    }

    [Fact]
    public void Accept_WhenRequestHasOffersReceived_ShouldChangeStatusAndSetOfferFulfillment()
    {
        // Arrange
        var request = CreateRequest();

        request.Publish();
        request.MarkOffersReceived();

        // Act
        request.Accept();

        // Assert
        Assert.Equal(TransportRequestStatus.Accepted, request.Status);
        Assert.Equal(FulfillmentMode.Offer, request.Fulfillment);
    }
    [Fact]
    public void AssignToTrip_WhenRequestIsPublished_ShouldChangeStatusAndSetTripFulfillment()
    {
        // Arrange
        var request = CreateRequest();

        request.Publish();

        // Act
        request.AssignToTrip();

        // Assert
        Assert.Equal(TransportRequestStatus.Accepted, request.Status);
        Assert.Equal(FulfillmentMode.Trip, request.Fulfillment);
    }

    [Fact]
    public void AssignToTrip_WhenRequestIsMatching_ShouldChangeStatusAndSetTripFulfillment()
    {
        // Arrange
        var request = CreateRequest();

        request.Publish();
        request.StartMatching();

        // Act
        request.AssignToTrip();

        // Assert
        Assert.Equal(TransportRequestStatus.Accepted, request.Status);
        Assert.Equal(FulfillmentMode.Trip, request.Fulfillment);
    }

    [Fact]
    public void Start_WhenRequestIsAccepted_ShouldChangeStatusToInProgress()
    {
        // Arrange
        var request = CreateRequest();

        request.Publish();
        request.AssignToTrip();

        // Act
        request.Start();

        // Assert
        Assert.Equal(TransportRequestStatus.InProgress, request.Status);
    }

    [Fact]
    public void Complete_WhenRequestIsInProgress_ShouldChangeStatusToCompleted()
    {
        // Arrange
        var request = CreateRequest();

        request.Publish();
        request.AssignToTrip();
        request.Start();

        // Act
        request.Complete();

        // Assert
        Assert.Equal(TransportRequestStatus.Completed, request.Status);
    }

    [Fact]
    public void Start_WhenRequestIsPublished_ShouldThrow()
    {
        // Arrange
        var request = CreateRequest();

        request.Publish();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => request.Start());

        // Assert
        Assert.Equal(
            "Only accepted requests can be started.",
            exception.Message);
    }

    [Fact]
    public void Start_WhenRequestIsDraft_ShouldThrow()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => request.Start());

        // Assert
        Assert.Equal(
            "Only accepted requests can be started.",
            exception.Message);
    }

    [Fact]
    public void Start_WhenRequestIsMatching_ShouldThrow()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();
        request.StartMatching();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => request.Start());

        // Assert
        Assert.Equal(
            "Only accepted requests can be started.",
            exception.Message);
    }

    [Fact]
    public void Start_WhenRequestIsOffersReceived_ShouldThrow()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();
        request.MarkOffersReceived();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => request.Start());

        // Assert
        Assert.Equal(
            "Only accepted requests can be started.",
            exception.Message);
    }

    [Fact]
    public void Complete_WhenRequestIsDraft_ShouldThrow()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => request.Complete());

        // Assert
        Assert.Equal(
            "Only in-progress requests can be completed.",
            exception.Message);
    }

    [Fact]
    public void Complete_WhenRequestIsAccepted_ShouldThrow()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();
        request.AssignToTrip();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => request.Complete());

        // Assert
        Assert.Equal(
            "Only in-progress requests can be completed.",
            exception.Message);
    }

    [Fact]
    public void Complete_WhenRequestIsCompleted_ShouldThrow()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();
        request.AssignToTrip();
        request.Start();
        request.Complete();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => request.Complete());

        // Assert
        Assert.Equal(
            "Only in-progress requests can be completed.",
            exception.Message);
    }

    [Fact]
    public void Expire_WhenRequestIsPublished_ShouldChangeStatusToExpired()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();

        // Act
        request.Expire();

        // Assert
        Assert.Equal(
            TransportRequestStatus.Expired,
            request.Status);
    }

    [Fact]
    public void Expire_WhenRequestIsMatching_ShouldChangeStatusToExpired()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();
        request.StartMatching();

        // Act
        request.Expire();

        // Assert
        Assert.Equal(
            TransportRequestStatus.Expired,
            request.Status);
    }

    [Fact]
    public void Expire_WhenRequestHasOffersReceived_ShouldChangeStatusToExpired()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();
        request.MarkOffersReceived();

        // Act
        request.Expire();

        // Assert
        Assert.Equal(
            TransportRequestStatus.Expired,
            request.Status);
    }

    [Fact]
    public void Expire_WhenRequestIsDraft_ShouldThrow()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => request.Expire());

        // Assert
        Assert.Equal(
            "Only active requests can expire.",
            exception.Message);
    }

    [Fact]
    public void Expire_WhenRequestIsAccepted_ShouldThrow()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();
        request.AssignToTrip();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => request.Expire());

        // Assert
        Assert.Equal(
            "Only active requests can expire.",
            exception.Message);
    }

    [Fact]
    public void ReturnToPublished_WhenRequestIsAccepted_ShouldReturnToPublishedAndClearFulfillment()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();
        request.AssignToTrip();

        // Act
        request.ReturnToPublished();

        // Assert
        Assert.Equal(
            TransportRequestStatus.Published,
            request.Status);

        Assert.Null(request.Fulfillment);
    }

    [Fact]
    public void ReturnToPublished_WhenRequestIsPublished_ShouldThrow()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => request.ReturnToPublished());

        // Assert
        Assert.Equal(
            "Only accepted requests can be returned to published.",
            exception.Message);
    }

    [Fact]
    public void Cancel_WhenRequestIsPublished_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();

        // Act
        request.Cancel();

        // Assert
        Assert.Equal(
            TransportRequestStatus.Cancelled,
            request.Status);
    }

    [Fact]
    public void Cancel_WhenRequestIsInProgress_ShouldThrow()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();
        request.AssignToTrip();
        request.Start();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => request.Cancel());

        // Assert
        Assert.Equal(
            "In-progress requests cannot be cancelled.",
            exception.Message);
    }

    [Fact]
    public void Cancel_WhenRequestIsCompleted_ShouldThrow()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();
        request.AssignToTrip();
        request.Start();
        request.Complete();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => request.Cancel());

        // Assert
        Assert.Equal(
            "Completed requests cannot be cancelled.",
            exception.Message);
    }

    [Fact]
    public void Cancel_WhenRequestIsCancelled_ShouldThrow()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();
        request.Cancel();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => request.Cancel());

        // Assert
        Assert.Equal(
            "The request is already cancelled.",
            exception.Message);
    }

    [Fact]
    public void Cancel_WhenRequestIsExpired_ShouldThrow()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();
        request.Expire();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => request.Cancel());

        // Assert
        Assert.Equal(
            "Expired requests cannot be cancelled.",
            exception.Message);
    }

    [Fact]
    public void Cancel_WhenRequestIsMatching_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();
        request.StartMatching();

        // Act
        request.Cancel();

        // Assert
        Assert.Equal(
            TransportRequestStatus.Cancelled,
            request.Status);
    }

    [Fact]
    public void Cancel_WhenRequestHasOffersReceived_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();
        request.MarkOffersReceived();

        // Act
        request.Cancel();

        // Assert
        Assert.Equal(
            TransportRequestStatus.Cancelled,
            request.Status);
    }

    [Fact]
    public void ReturnToPublished_WhenRequestWasAcceptedByOffer_ShouldClearOfferFulfillment()
    {
        // Arrange
        var request = CreateRequest();
        request.Publish();
        request.MarkOffersReceived();
        request.Accept();

        // Act
        request.ReturnToPublished();

        // Assert
        Assert.Equal(
            TransportRequestStatus.Published,
            request.Status);

        Assert.Null(request.Fulfillment);
    }

    private static TransportRequest CreateRequest()
    {
        return new TransportRequest(
            customerId: Guid.NewGuid(),
            pickupLocationId: Guid.NewGuid(),
            deliveryLocationId: Guid.NewGuid(),
            pickupDate: DateTime.UtcNow.AddDays(1),
            deliveryDate: DateTime.UtcNow.AddDays(2));
    }
}