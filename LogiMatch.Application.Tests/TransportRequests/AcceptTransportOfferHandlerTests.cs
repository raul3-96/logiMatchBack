using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.TransportOffers;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.TransportOffers;

public class AcceptTransportOfferHandlerTests
{
    private static User CreateUser(string email = "user@example.com")
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

    private static Location CreateLocation(
        string address,
        string city,
        string postalCode)
    {
        return new Location(
            address,
            city,
            postalCode,
            "Spain",
            37.3772,
            -5.9869);
    }

    private static TransportRequest CreatePublishedRequest(
        Guid customerId,
        Guid pickupLocationId,
        Guid deliveryLocationId)
    {
        var request = new TransportRequest(
            customerId,
            pickupLocationId,
            deliveryLocationId,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));

        request.Publish();
        request.MarkOffersReceived();

        return request;
    }

    private static VehicleAvailability CreateAvailability(Guid vehicleId)
    {
        return new VehicleAvailability(
            vehicleId,
            DateTime.UtcNow.AddHours(12),
            DateTime.UtcNow.AddDays(3));
    }

    private static TransportOffer CreatePendingOffer(
        Guid requestId,
        Guid transporterProfileId,
        Guid vehicleId)
    {
        return new TransportOffer(
            requestId,
            transporterProfileId,
            vehicleId,
            350m,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));
    }

    private static AcceptTransportOfferHandler CreateHandler(
        IApplicationDbContext db,
        Guid currentUserId)
    {
        return new AcceptTransportOfferHandler(
            db,
            new FakeCurrentUserService(currentUserId));
    }

    [Fact]
    public async Task Handle_WhenCurrentUserOwnsRequest_ShouldCreateBooking()
    {
        var db = TestDbContextFactory.Create();

        var customer = CreateUser("customer@example.com");
        var transporterUser = CreateUser("transporter@example.com");
        var transporter = CreateTransporter(transporterUser.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var pickup = CreateLocation(
            "Plaza de España",
            "Sevilla",
            "41013");

        var delivery = CreateLocation(
            "Puerta del Sol",
            "Madrid",
            "28013");

        var request = CreatePublishedRequest(
            customer.Id,
            pickup.Id,
            delivery.Id);

        var availability = CreateAvailability(vehicle.Id);

        var offer = CreatePendingOffer(
            request.Id,
            transporter.Id,
            vehicle.Id);

        db.Users.Add(customer);
        db.Users.Add(transporterUser);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Locations.Add(pickup);
        db.Locations.Add(delivery);
        db.TransportRequests.Add(request);
        db.VehicleAvailabilities.Add(availability);
        db.TransportOffers.Add(offer);

        await db.SaveChangesAsync();

        var handler = CreateHandler(db, customer.Id);

        var bookingId = await handler.Handle(offer.Id);

        var booking = await db.Bookings.FindAsync(bookingId);

        Assert.NotEqual(Guid.Empty, bookingId);
        Assert.NotNull(booking);
        Assert.Equal(offer.Id, booking!.TransportOfferId);
        Assert.Equal(request.Id, booking.TransportRequestId);
        Assert.Equal(TransportOfferStatus.Accepted, offer.Status);
        Assert.Equal(TransportRequestStatus.Accepted, request.Status);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserDoesNotOwnRequest_ShouldThrow()
    {
        var db = TestDbContextFactory.Create();

        var customer = CreateUser("customer@example.com");
        var otherUser = CreateUser("other@example.com");
        var transporterUser = CreateUser("transporter@example.com");
        var transporter = CreateTransporter(transporterUser.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var pickup = CreateLocation(
            "Plaza de España",
            "Sevilla",
            "41013");

        var delivery = CreateLocation(
            "Puerta del Sol",
            "Madrid",
            "28013");

        var request = CreatePublishedRequest(
            customer.Id,
            pickup.Id,
            delivery.Id);

        var availability = CreateAvailability(vehicle.Id);

        var offer = CreatePendingOffer(
            request.Id,
            transporter.Id,
            vehicle.Id);

        db.Users.Add(customer);
        db.Users.Add(otherUser);
        db.Users.Add(transporterUser);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Locations.Add(pickup);
        db.Locations.Add(delivery);
        db.TransportRequests.Add(request);
        db.VehicleAvailabilities.Add(availability);
        db.TransportOffers.Add(offer);

        await db.SaveChangesAsync();

        var handler = CreateHandler(db, otherUser.Id);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(offer.Id));

        Assert.Equal(
            "Only the owner of the transport request can accept offers.",
            exception.Message);
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