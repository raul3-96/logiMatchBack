using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.TransportRequests;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace LogiMatch.Application.Tests.TransportRequests;

public class CancelTransportRequestHandlerTests
{
    [Fact]
    public async Task Handle_WhenRequestDoesNotExist_ShouldThrow()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();
        var handler = new CancelTransportRequestHandler(db,new MockCurrentUserService(Guid.NewGuid()));

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(Guid.NewGuid()));

        // Assert
        Assert.Equal(
            "The specified transport request does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestIsInProgress_ShouldThrow()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();

        var user = new MockCurrentUserService(Guid.NewGuid());
        var request = CreateRequest(user.UserId);
        request.Publish();
        request.StartMatching();
        request.MarkOffersReceived();
        request.Accept();
        request.Start();

        db.TransportRequests.Add(request);
        await db.SaveChangesAsync();

        var handler = new CancelTransportRequestHandler(db, user);

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(request.Id));

        // Assert
        Assert.Equal(
            "In-progress transport requests cannot be cancelled.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestIsCompleted_ShouldThrow()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();

        var user = new MockCurrentUserService(Guid.NewGuid());
        var request = CreateRequest(user.UserId);
        request.Publish();
        request.StartMatching();
        request.MarkOffersReceived();
        request.Accept();
        request.Start();
        request.Complete();

        db.TransportRequests.Add(request);
        await db.SaveChangesAsync();

        var handler = new CancelTransportRequestHandler(db, user);

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(request.Id));

        // Assert
        Assert.Equal(
            "Completed transport requests cannot be cancelled.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestIsAlreadyCancelled_ShouldThrow()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();

        var user = new MockCurrentUserService(Guid.NewGuid());
        var request = CreateRequest(user.UserId);
        request.Publish();
        request.Cancel();

        db.TransportRequests.Add(request);
        await db.SaveChangesAsync();

        var handler = new CancelTransportRequestHandler(db, user);

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(request.Id));

        // Assert
        Assert.Equal(
            "The transport request is already cancelled.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestIsExpired_ShouldThrow()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();

        var user = new MockCurrentUserService(Guid.NewGuid());
        var request = CreateRequest(user.UserId);
        request.Publish();
        request.Expire();

        db.TransportRequests.Add(request);
        await db.SaveChangesAsync();

        var handler = new CancelTransportRequestHandler(db, user);

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(request.Id));

        // Assert
        Assert.Equal(
            "Expired transport requests cannot be cancelled.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenActiveBookingExists_ShouldThrow()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();

        var user= new MockCurrentUserService(Guid.NewGuid());
        var request = CreateRequest(user.UserId);
        request.Publish();
        request.StartMatching();
        request.MarkOffersReceived();
        request.Accept();

        var offer = new TransportOffer(
            request.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            350m,
            request.PickupDate,
            request.DeliveryDate!.Value);

        var booking = new Booking(
            request.Id,
            offer.Id);

        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CancelTransportRequestHandler(db, user);

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(request.Id));

        // Assert
        Assert.Equal(
            "The transport request has an active booking and must be cancelled through the booking.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenBookingIsCancelled_ShouldAllowCancellation()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();

        var user = new MockCurrentUserService(Guid.NewGuid());
        var request = CreateRequest(user.UserId);
        request.Publish();

        var offer = new TransportOffer(
            request.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            350m,
            request.PickupDate,
            request.DeliveryDate!.Value);

        var booking = new Booking(
            request.Id,
            offer.Id);

        booking.Cancel();

        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CancelTransportRequestHandler(db, user);

        // Act
        await handler.Handle(request.Id);

        // Assert
        Assert.Equal(
            TransportRequestStatus.Cancelled,
            request.Status);
    }

    [Fact]
    public async Task Handle_WhenActiveTripCargoExistsButTripDoesNotExist_ShouldThrow()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();

        var user= new MockCurrentUserService(Guid.NewGuid());
        var request = CreateRequest(user.UserId);
        request.Publish();
        request.AssignToTrip();

        var tripCargo = new TripCargo(
            Guid.NewGuid(),
            request.Id,
            500m,
            2m);

        db.TransportRequests.Add(request);
        db.TripCargos.Add(tripCargo);

        await db.SaveChangesAsync();

        var handler = new CancelTransportRequestHandler(db, user);

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(request.Id));

        // Assert
        Assert.Equal(
            "The trip associated with the transport request does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenTripIsNotPublished_ShouldThrow()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();

        var user = new MockCurrentUserService(Guid.NewGuid());
        var request = CreateRequest(user.UserId);
        request.Publish();
        request.AssignToTrip();

        var trip = CreateTrip();
        trip.Start();

        var tripCargo = new TripCargo(
            trip.Id,
            request.Id,
            500m,
            2m);

        db.TransportRequests.Add(request);
        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);

        await db.SaveChangesAsync();

        var handler = new CancelTransportRequestHandler(db, user);

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(request.Id));

        // Assert
        Assert.Equal(
            "A transport request with a reserved trip cargo can only be cancelled while the trip is published.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestIsPublishedWithoutDependencies_ShouldCancelRequest()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();

        var user = new MockCurrentUserService(Guid.NewGuid());
        var request = CreateRequest(user.UserId);
        request.Publish();

        db.TransportRequests.Add(request);
        await db.SaveChangesAsync();

        var handler = new CancelTransportRequestHandler(db, user);

        // Act
        await handler.Handle(request.Id);

        // Assert
        Assert.Equal(
            TransportRequestStatus.Cancelled,
            request.Status);

        var savedRequest = await db.TransportRequests
            .FirstAsync(x => x.Id == request.Id);

        Assert.Equal(
            TransportRequestStatus.Cancelled,
            savedRequest.Status);
    }

    [Fact]
    public async Task Handle_WhenTripCargoExists_ShouldCancelCargoReleaseCapacityAndCancelRequest()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();

        var user = new MockCurrentUserService(Guid.NewGuid());
        var request = CreateRequest(user.UserId);
        request.Publish();
        request.AssignToTrip();

        var trip = CreateTrip();

        var tripCargo = new TripCargo(
            trip.Id,
            request.Id,
            500m,
            2m);

        trip.ReserveCapacity(500m, 2m);

        db.TransportRequests.Add(request);
        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);

        await db.SaveChangesAsync();

        var handler = new CancelTransportRequestHandler(db, user);

        // Act
        await handler.Handle(request.Id);

        // Assert
        Assert.Equal(
            TransportRequestStatus.Cancelled,
            request.Status);

        Assert.Equal(
            TripCargoStatus.Cancelled,
            tripCargo.Status);

        Assert.Equal(
            trip.InitialAvailableWeightKg,
            trip.AvailableWeightKg);

        Assert.Equal(
            trip.InitialAvailableVolumeM3,
            trip.AvailableVolumeM3);

        var savedTripCargo = await db.TripCargos
            .FirstAsync(x => x.Id == tripCargo.Id);

        Assert.Equal(
            TripCargoStatus.Cancelled,
            savedTripCargo.Status);

        var savedTrip = await db.Trips
            .FirstAsync(x => x.Id == trip.Id);

        Assert.Equal(
            trip.InitialAvailableWeightKg,
            savedTrip.AvailableWeightKg);

        Assert.Equal(
            trip.InitialAvailableVolumeM3,
            savedTrip.AvailableVolumeM3);
    }

    [Fact]
    public async Task Handle_WhenCancelledTripCargoExists_ShouldIgnoreItAndCancelRequest()
    {
        // Arrange
        await using var db = TestDbContextFactory.Create();

        var user = new MockCurrentUserService(Guid.NewGuid());
        var request = CreateRequest(user.UserId);
        request.Publish();

        var trip = CreateTrip();

        var tripCargo = new TripCargo(
            trip.Id,
            request.Id,
            500m,
            2m);

        tripCargo.Cancel();

        db.TransportRequests.Add(request);
        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);

        await db.SaveChangesAsync();

        var handler = new CancelTransportRequestHandler(db, user);

        // Act
        await handler.Handle(request.Id);

        // Assert
        Assert.Equal(
            TransportRequestStatus.Cancelled,
            request.Status);

        Assert.Equal(
            TripCargoStatus.Cancelled,
            tripCargo.Status);

        Assert.Equal(
            trip.InitialAvailableWeightKg,
            trip.AvailableWeightKg);

        Assert.Equal(
            trip.InitialAvailableVolumeM3,
            trip.AvailableVolumeM3);
    }

    private static TransportRequest CreateRequest(Guid userId) =>
        new TransportRequest(
            userId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(3));

    private static Trip CreateTrip() =>        
        new Trip(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(8),
            1500m,
            10m);
}