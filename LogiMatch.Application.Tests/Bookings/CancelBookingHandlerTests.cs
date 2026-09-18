using LogiMatch.Application.Bookings;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMatch.Application.Tests.Bookings;

public class CancelBookingHandlerTests
{
    private static User CreateUser(
        string email = "john@example.com")
    {
        return new User(
            email,
            "John",
            "Doe",
            "600123456");
    }

    private static TransporterProfile CreateTransporter(Guid userId)
    {
        return new TransporterProfile(userId);
    }

    private static Vehicle CreateVehicle(Guid transporterProfileId)
    {
        return new Vehicle(
            transporterProfileId,
            VehicleType.LargeVan,
            "Mercedes-Benz",
            "Sprinter",
            "1234ABC",
            1500m,
            10m,
            6m,
            2m,
            2.5m,
            true,
            false);
    }

    private static TransportRequest CreateAcceptedRequest()
    {
        var request = new TransportRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));

        request.Publish();
        request.MarkOffersReceived();
        request.Accept();

        return request;
    }

    private static TransportOffer CreateAcceptedOffer(
        Guid requestId,
        Guid transporterProfileId,
        Guid vehicleId)
    {
        var offer = new TransportOffer(
            requestId,
            transporterProfileId,
            vehicleId,
            350m,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));

        offer.Accept();

        return offer;
    }

    private static Booking CreateBooking(
        Guid requestId,
        Guid offerId)
    {
        return new Booking(requestId, offerId);
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenBookingDoesNotExist()
    {
        var db = TestDbContextFactory.Create();
        var handler = new CancelBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(Guid.NewGuid()));

        Assert.Equal(
            "The specified booking does not exist.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenBookingIsNotConfirmed()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var request = CreateAcceptedRequest();

        var offer = CreateAcceptedOffer(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var booking = CreateBooking(
            request.Id,
            offer.Id);

        booking.Start();

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CancelBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "Only confirmed bookings can be cancelled.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenRequestDoesNotExist()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var requestId = Guid.NewGuid();

        var offer = CreateAcceptedOffer(
            requestId,
            transporter.Id,
            vehicle.Id);

        var booking = CreateBooking(
            requestId,
            offer.Id);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CancelBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "The transport request associated with the booking does not exist.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenRequestIsNotAccepted()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var request = new TransportRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));

        request.Publish();
        request.MarkOffersReceived();

        var offer = CreateAcceptedOffer(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var booking = CreateBooking(
            request.Id,
            offer.Id);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CancelBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "Only accepted transport requests can have their booking cancelled.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenOfferDoesNotExist()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var request = CreateAcceptedRequest();

        var offerId = Guid.NewGuid();

        var booking = CreateBooking(
            request.Id,
            offerId);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CancelBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "The transport offer associated with the booking does not exist.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenOfferDoesNotBelongToRequest()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var request = CreateAcceptedRequest();
        var otherRequest = CreateAcceptedRequest();

        var offer = CreateAcceptedOffer(
            otherRequest.Id,
            transporter.Id,
            vehicle.Id);

        var booking = CreateBooking(
            request.Id,
            offer.Id);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportRequests.Add(otherRequest);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CancelBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "The transport offer does not belong to the transport request.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenOfferIsNotAccepted()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var request = CreateAcceptedRequest();

        var offer = new TransportOffer(
            request.Id,
            transporter.Id,
            vehicle.Id,
            350m,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));

        var booking = CreateBooking(
            request.Id,
            offer.Id);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CancelBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "Only accepted transport offers can have a booking cancelled.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldCancelBookingAndReturnRequestToPublished()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var request = CreateAcceptedRequest();

        var offer = CreateAcceptedOffer(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var booking = CreateBooking(
            request.Id,
            offer.Id);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CancelBookingHandler(db);

        await handler.Handle(booking.Id);

        Assert.Equal(
            BookingStatus.Cancelled,
            booking.Status);

        Assert.Equal(
            TransportRequestStatus.Published,
            request.Status);

        Assert.Null(request.Fulfillment);
    }


    [Fact]
    public async Task Handle_ShouldPersistCancelledBookingAndPublishedRequest()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var request = CreateAcceptedRequest();

        var offer = CreateAcceptedOffer(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var booking = CreateBooking(
            request.Id,
            offer.Id);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CancelBookingHandler(db);

        await handler.Handle(booking.Id);

        var savedBooking = await db.Bookings
            .FirstAsync(x => x.Id == booking.Id);

        var savedRequest = await db.TransportRequests
            .FirstAsync(x => x.Id == request.Id);

        Assert.Equal(
            BookingStatus.Cancelled,
            savedBooking.Status);

        Assert.Equal(
            TransportRequestStatus.Published,
            savedRequest.Status);

        Assert.Null(savedRequest.Fulfillment);
    }
}