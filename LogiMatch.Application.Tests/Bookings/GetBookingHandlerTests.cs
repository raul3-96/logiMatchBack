using LogiMatch.Application.Bookings;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.Bookings;

public class GetBookingHandlerTests
{
    private static User CreateUser()
    {
        return new User(
            "john@example.com",
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


    [Fact]
    public async Task Handle_ShouldReturnNull_WhenBookingDoesNotExist()
    {
        var db = TestDbContextFactory.Create();

        var handler = new GetBookingHandler(db);

        var result = await handler.Handle(Guid.NewGuid());

        Assert.Null(result);
    }


    [Fact]
    public async Task Handle_ShouldReturnBooking_WhenBookingExists()
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

        var handler = new GetBookingHandler(db);

        var result = await handler.Handle(booking.Id);

        Assert.NotNull(result);

        var type = result!.GetType();

        Assert.Equal(
            booking.Id,
            type.GetProperty("Id")!.GetValue(result));

        Assert.Equal(
            booking.Status,
            type.GetProperty("Status")!.GetValue(result));

        Assert.Equal(
            booking.CreatedAt,
            type.GetProperty("CreatedAt")!.GetValue(result));
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

        var handler = new GetBookingHandler(db);

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

        var handler = new GetBookingHandler(db);

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

        var handler = new GetBookingHandler(db);

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
}