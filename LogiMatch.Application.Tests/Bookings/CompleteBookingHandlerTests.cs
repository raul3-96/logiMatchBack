using LogiMatch.Application.Bookings;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.Bookings;

public class CompleteBookingHandlerTests
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

    private static TransportRequest CreateInProgressRequest()
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
        request.Start();

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

    private static Booking CreateInProgressBooking(
        Guid requestId,
        Guid offerId)
    {
        var booking = new Booking(requestId, offerId);
        booking.Start();
        return booking;
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenBookingDoesNotExist()
    {
        var db = TestDbContextFactory.Create();
        var handler = new CompleteBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(Guid.NewGuid()));

        Assert.Equal(
            "The specified booking does not exist.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenBookingIsNotInProgress()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);
        var vehicle = CreateVehicle(transporter.Id);
        var request = CreateInProgressRequest();

        var offer = CreateAcceptedOffer(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var booking = new Booking(
            request.Id,
            offer.Id);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CompleteBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "Only in-progress bookings can be completed.",
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

        var booking = CreateInProgressBooking(
            requestId,
            offer.Id);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CompleteBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "The transport request associated with the booking does not exist.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenRequestIsNotInProgress()
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
        request.Accept();

        var offer = CreateAcceptedOffer(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var booking = CreateInProgressBooking(
            request.Id,
            offer.Id);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CompleteBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "Only in-progress transport requests can be completed.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenOfferDoesNotExist()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var request = CreateInProgressRequest();

        var offerId = Guid.NewGuid();

        var booking = CreateInProgressBooking(
            request.Id,
            offerId);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CompleteBookingHandler(db);

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

        var request = CreateInProgressRequest();
        var otherRequest = CreateInProgressRequest();

        var offer = CreateAcceptedOffer(
            otherRequest.Id,
            transporter.Id,
            vehicle.Id);

        var booking = CreateInProgressBooking(
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

        var handler = new CompleteBookingHandler(db);

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

        var request = CreateInProgressRequest();

        var offer = new TransportOffer(
            request.Id,
            transporter.Id,
            vehicle.Id,
            350m,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));

        var booking = CreateInProgressBooking(
            request.Id,
            offer.Id);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CompleteBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "Only accepted transport offers can be completed.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldCompleteBookingAndRequest_WhenAllConditionsAreValid()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var request = CreateInProgressRequest();

        var offer = CreateAcceptedOffer(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var booking = CreateInProgressBooking(
            request.Id,
            offer.Id);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CompleteBookingHandler(db);

        await handler.Handle(booking.Id);

        Assert.Equal(
            BookingStatus.Completed,
            booking.Status);

        Assert.Equal(
            TransportRequestStatus.Completed,
            request.Status);
    }
}