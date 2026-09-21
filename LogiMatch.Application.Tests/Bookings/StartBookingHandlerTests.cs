using LogiMatch.Application.Bookings;
using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.Bookings;

public class StartBookingHandlerTests
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

    private static TransportRequest CreateAcceptedRequest(
        Guid customerId)
    {
        var request = new TransportRequest(
            customerId,
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

    private static Booking CreateConfirmedBooking(
        Guid requestId,
        Guid offerId)
    {
        var booking = new Booking(requestId, offerId);
        booking.Complete();
        return booking;
    }

    private static StartBookingHandler CreateHandler(
        IApplicationDbContext db,
        Guid currentUserId)
    {
        return new StartBookingHandler(
            db,
            new FakeCurrentUserService(currentUserId));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenBookingDoesNotExist()
    {
        var db = TestDbContextFactory.Create();
        var handler = CreateHandler(db, Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<NotFoundException>(
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

        var request = CreateAcceptedRequest(user.Id);

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

        var handler = CreateHandler(db, user.Id);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "Only confirmed bookings can be started.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenCurrentUserDoesNotOwnRequest()
    {
        var db = TestDbContextFactory.Create();

        var customer = CreateUser("customer@example.com");
        var otherUser = CreateUser("other@example.com");
        var transporter = CreateTransporter(customer.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var request = CreateAcceptedRequest(customer.Id);

        var offer = CreateAcceptedOffer(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var booking = CreateConfirmedBooking(
            request.Id,
            offer.Id);

        db.Users.Add(customer);
        db.Users.Add(otherUser);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = CreateHandler(db, otherUser.Id);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "Only the owner of the transport request can start the booking.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldStartBookingAndRequest_WhenAllConditionsAreValid()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var request = CreateAcceptedRequest(user.Id);

        var offer = CreateAcceptedOffer(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var booking = CreateConfirmedBooking(
            request.Id,
            offer.Id);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = CreateHandler(db, user.Id);

        await handler.Handle(booking.Id);

        Assert.Equal(
            BookingStatus.InProgress,
            booking.Status);

        Assert.Equal(
            TransportRequestStatus.InProgress,
            request.Status);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId)
        {
            UserId = userId;
        }

        public Guid UserId { get; }
    }
}