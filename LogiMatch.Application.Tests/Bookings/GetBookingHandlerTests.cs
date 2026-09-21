using LogiMatch.Application.Bookings;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.Bookings;

public class GetBookingHandlerTests
{
    private static User CreateUser(string email = "john@example.com")
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

    private static TransportRequest CreateAcceptedRequest(
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

    private static GetBookingHandler CreateHandler(
        IApplicationDbContext db,
        Guid currentUserId)
    {
        return new GetBookingHandler(
            db,
            new FakeCurrentUserService(currentUserId));
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenBookingDoesNotExist()
    {
        var db = TestDbContextFactory.Create();
        var user = CreateUser();

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, user.Id);

        var result = await handler.Handle(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnBooking_WhenCurrentUserIsCustomerOwner()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var pickup = CreateLocation(
            "Plaza de España",
            "Sevilla",
            "41013");

        var delivery = CreateLocation(
            "Puerta del Sol",
            "Madrid",
            "28013");

        var request = CreateAcceptedRequest(
            user.Id,
            pickup.Id,
            delivery.Id);

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
        db.Locations.Add(pickup);
        db.Locations.Add(delivery);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = CreateHandler(db, user.Id);

        var result = await handler.Handle(booking.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnBooking_WhenCurrentUserIsTransporterOwner()
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

        var request = CreateAcceptedRequest(
            customer.Id,
            pickup.Id,
            delivery.Id);

        var offer = CreateAcceptedOffer(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var booking = new Booking(
            request.Id,
            offer.Id);

        db.Users.Add(customer);
        db.Users.Add(transporterUser);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Locations.Add(pickup);
        db.Locations.Add(delivery);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = CreateHandler(db, transporterUser.Id);

        var result = await handler.Handle(booking.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenCurrentUserDoesNotOwnBooking()
    {
        var db = TestDbContextFactory.Create();

        var customer = CreateUser();
        var transporter = CreateTransporter(customer.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var pickup = CreateLocation(
            "Plaza de España",
            "Sevilla",
            "41013");

        var delivery = CreateLocation(
            "Puerta del Sol",
            "Madrid",
            "28013");

        var request = CreateAcceptedRequest(
            customer.Id,
            pickup.Id,
            delivery.Id);

        var offer = CreateAcceptedOffer(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var booking = new Booking(
            request.Id,
            offer.Id);

        db.Users.Add(customer);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Locations.Add(pickup);
        db.Locations.Add(delivery);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = CreateHandler(db, Guid.NewGuid());

        var result = await handler.Handle(booking.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAll_ShouldReturnOnlyBookingsOwnedByCurrentUser()
    {
        var db = TestDbContextFactory.Create();

        var currentUser = CreateUser("current@example.com");
        var otherUser = CreateUser("other@example.com");
        var transporterUser = CreateUser("transporter@example.com");
        var transporter = CreateTransporter(transporterUser.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var currentPickup = CreateLocation(
            "Plaza de España",
            "Sevilla",
            "41013");

        var currentDelivery = CreateLocation(
            "Puerta del Sol",
            "Madrid",
            "28013");

        var otherPickup = CreateLocation(
            "Calle Larios",
            "Málaga",
            "29005");

        var otherDelivery = CreateLocation(
            "Gran Vía",
            "Madrid",
            "28013");

        var currentRequest = CreateAcceptedRequest(
            currentUser.Id,
            currentPickup.Id,
            currentDelivery.Id);

        var otherRequest = CreateAcceptedRequest(
            otherUser.Id,
            otherPickup.Id,
            otherDelivery.Id);

        var currentOffer = CreateAcceptedOffer(
            currentRequest.Id,
            transporter.Id,
            vehicle.Id);

        var otherOffer = CreateAcceptedOffer(
            otherRequest.Id,
            transporter.Id,
            vehicle.Id);

        var currentBooking = new Booking(
            currentRequest.Id,
            currentOffer.Id);

        var otherBooking = new Booking(
            otherRequest.Id,
            otherOffer.Id);

        db.Users.Add(currentUser);
        db.Users.Add(otherUser);
        db.Users.Add(transporterUser);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Locations.Add(currentPickup);
        db.Locations.Add(currentDelivery);
        db.Locations.Add(otherPickup);
        db.Locations.Add(otherDelivery);
        db.TransportRequests.Add(currentRequest);
        db.TransportRequests.Add(otherRequest);
        db.TransportOffers.Add(currentOffer);
        db.TransportOffers.Add(otherOffer);
        db.Bookings.Add(currentBooking);
        db.Bookings.Add(otherBooking);

        await db.SaveChangesAsync();

        var handler = CreateHandler(db, currentUser.Id);

        var result = await handler.HandleAll();

        var items = result.GetType()
            .GetProperty("items")!
            .GetValue(result) as System.Collections.IEnumerable;

        var list = items!.Cast<object>().ToList();

        Assert.Single(list);

        var itemType = list[0].GetType();

        Assert.Equal(
            currentBooking.Id,
            itemType.GetProperty("Id")!.GetValue(list[0]));
    }

    [Fact]
    public async Task Handle_ShouldReturnTransportRequestInformation()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var pickup = CreateLocation(
            "Plaza de España",
            "Sevilla",
            "41013");

        var delivery = CreateLocation(
            "Puerta del Sol",
            "Madrid",
            "28013");

        var request = CreateAcceptedRequest(
            user.Id,
            pickup.Id,
            delivery.Id);

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
        db.Locations.Add(pickup);
        db.Locations.Add(delivery);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = CreateHandler(db, user.Id);

        var result = await handler.Handle(booking.Id);

        Assert.NotNull(result);

        var transportRequest =
            result!.GetType()
                .GetProperty("TransportRequest")!
                .GetValue(result);

        Assert.NotNull(transportRequest);

        var requestType = transportRequest!.GetType();

        Assert.Equal(
            request.Id,
            requestType.GetProperty("Id")!.GetValue(transportRequest));

        Assert.Equal(
            request.CustomerId,
            requestType.GetProperty("CustomerId")!.GetValue(transportRequest));

        Assert.Equal(
            request.PickupDate,
            requestType.GetProperty("PickupDate")!.GetValue(transportRequest));

        Assert.Equal(
            request.DeliveryDate,
            requestType.GetProperty("DeliveryDate")!.GetValue(transportRequest));
    }

    [Fact]
    public async Task Handle_ShouldReturnPickupAndDeliveryLocations()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var pickup = CreateLocation(
            "Plaza de España",
            "Sevilla",
            "41013");

        var delivery = CreateLocation(
            "Puerta del Sol",
            "Madrid",
            "28013");

        var request = CreateAcceptedRequest(
            user.Id,
            pickup.Id,
            delivery.Id);

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
        db.Locations.Add(pickup);
        db.Locations.Add(delivery);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = CreateHandler(db, user.Id);

        var result = await handler.Handle(booking.Id);

        Assert.NotNull(result);

        var transportRequest =
            result!.GetType()
                .GetProperty("TransportRequest")!
                .GetValue(result);

        Assert.NotNull(transportRequest);

        var requestType = transportRequest!.GetType();

        var pickupResult =
            requestType.GetProperty("PickupLocation")!
                .GetValue(transportRequest);

        var deliveryResult =
            requestType.GetProperty("DeliveryLocation")!
                .GetValue(transportRequest);

        Assert.NotNull(pickupResult);
        Assert.NotNull(deliveryResult);

        Assert.Equal(
            pickup.Id,
            pickupResult!.GetType()
                .GetProperty("Id")!
                .GetValue(pickupResult));

        Assert.Equal(
            "Plaza de España",
            pickupResult.GetType()
                .GetProperty("Address")!
                .GetValue(pickupResult));

        Assert.Equal(
            "Sevilla",
            pickupResult.GetType()
                .GetProperty("City")!
                .GetValue(pickupResult));

        Assert.Equal(
            delivery.Id,
            deliveryResult!.GetType()
                .GetProperty("Id")!
                .GetValue(deliveryResult));

        Assert.Equal(
            "Puerta del Sol",
            deliveryResult.GetType()
                .GetProperty("Address")!
                .GetValue(deliveryResult));

        Assert.Equal(
            "Madrid",
            deliveryResult.GetType()
                .GetProperty("City")!
                .GetValue(deliveryResult));
    }

    [Fact]
    public async Task Handle_ShouldReturnOfferInformation()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);
        var vehicle = CreateVehicle(transporter.Id);

        var pickup = CreateLocation(
            "Plaza de España",
            "Sevilla",
            "41013");

        var delivery = CreateLocation(
            "Puerta del Sol",
            "Madrid",
            "28013");

        var request = CreateAcceptedRequest(
            user.Id,
            pickup.Id,
            delivery.Id);

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
        db.Locations.Add(pickup);
        db.Locations.Add(delivery);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = CreateHandler(db, user.Id);

        var result = await handler.Handle(booking.Id);

        Assert.NotNull(result);

        var offerResult =
            result!.GetType()
                .GetProperty("Offer")!
                .GetValue(result);

        Assert.NotNull(offerResult);

        var offerType = offerResult!.GetType();

        Assert.Equal(
            offer.Id,
            offerType.GetProperty("Id")!.GetValue(offerResult));

        Assert.Equal(
            offer.TransporterProfileId,
            offerType.GetProperty("TransporterProfileId")!.GetValue(offerResult));

        Assert.Equal(
            offer.VehicleId,
            offerType.GetProperty("VehicleId")!.GetValue(offerResult));

        Assert.Equal(
            offer.Price,
            offerType.GetProperty("Price")!.GetValue(offerResult));

        Assert.Equal(
            offer.Status,
            offerType.GetProperty("Status")!.GetValue(offerResult));

        Assert.Equal(
            offer.EstimatedPickupDate,
            offerType.GetProperty("EstimatedPickupDate")!.GetValue(offerResult));

        Assert.Equal(
            offer.EstimatedDeliveryDate,
            offerType.GetProperty("EstimatedDeliveryDate")!.GetValue(offerResult));
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