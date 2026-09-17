using LogiMatch.Application.Bookings;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.Bookings;

public class StartBookingHandlerTests
{
    private static User CreateUser()
    {
        return new User(
            "john@example.com",
            "John",
            "Doe",
            "600123456");
    }

    private static Company CreateCompany()
    {
        return new Company(
            "Transport Company",
            "B12345678",
            "company@example.com",
            "600123457");
    }

    private static TransporterProfile CreateTransporter(
        Guid userId,
        Guid? companyId = null)
    {
        return new TransporterProfile(userId, companyId);
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
        var handler = new StartBookingHandler(db);

        var bookingId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(bookingId));

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

        var handler = new StartBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "Only confirmed bookings can be started.",
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

        var handler = new StartBookingHandler(db);

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

        var handler = new StartBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "Only accepted transport requests can be started.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenRequestIsAssignedToTrip()
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

        var trip = new Trip(
            transporter.Id,
            vehicle.Id,
            request.PickupLocationId,
            request.DeliveryLocationId,
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(8),
            1500m,
            10m);

        var tripCargo = new TripCargo(
            trip.Id,
            request.Id,
            850m,
            4.5m);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);
        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);

        await db.SaveChangesAsync();

        var handler = new StartBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "The transport request is already assigned to a trip.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldNotThrow_WhenCancelledTripCargoExists()
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

        var trip = new Trip(
            transporter.Id,
            vehicle.Id,
            request.PickupLocationId,
            request.DeliveryLocationId,
            DateTime.UtcNow.AddDays(5),
            DateTime.UtcNow.AddDays(5).AddHours(8),
            1500m,
            10m);

        var tripCargo = new TripCargo(
            trip.Id,
            request.Id,
            850m,
            4.5m);

        tripCargo.Cancel();

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);
        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);

        await db.SaveChangesAsync();

        var handler = new StartBookingHandler(db);

        await handler.Handle(booking.Id);

        Assert.Equal(
            BookingStatus.InProgress,
            booking.Status);

        Assert.Equal(
            TransportRequestStatus.InProgress,
            request.Status);
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

        var handler = new StartBookingHandler(db);

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

        var handler = new StartBookingHandler(db);

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

        var handler = new StartBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "Only accepted transport offers can be started.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenVehicleDoesNotExist()
    {
        var db = TestDbContextFactory.Create();

        var user = CreateUser();
        var transporter = CreateTransporter(user.Id);

        var request = CreateAcceptedRequest();

        var vehicleId = Guid.NewGuid();

        var offer = CreateAcceptedOffer(
            request.Id,
            transporter.Id,
            vehicleId);

        var booking = CreateBooking(
            request.Id,
            offer.Id);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new StartBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "The vehicle associated with the transport offer does not exist.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenVehicleDoesNotBelongToOfferTransporter()
    {
        var db = TestDbContextFactory.Create();

        var user1 = CreateUser();

        var user2 = new User(
            "other@example.com",
            "Other",
            "User",
            "600123458");

        var transporter1 = CreateTransporter(user1.Id);
        var transporter2 = CreateTransporter(user2.Id);

        var vehicle = CreateVehicle(transporter2.Id);

        var request = CreateAcceptedRequest();

        var offer = CreateAcceptedOffer(
            request.Id,
            transporter1.Id,
            vehicle.Id);

        var booking = CreateBooking(
            request.Id,
            offer.Id);

        db.Users.Add(user1);
        db.Users.Add(user2);
        db.TransporterProfiles.Add(transporter1);
        db.TransporterProfiles.Add(transporter2);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new StartBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "The vehicle does not belong to the transporter who created the offer.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldThrow_WhenVehicleHasAnotherTripInProgress()
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

        var activeTrip = new Trip(
            transporter.Id,
            vehicle.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(3),
            DateTime.UtcNow.AddDays(3).AddHours(8),
            1500m,
            10m);

        activeTrip.Start();

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);
        db.Trips.Add(activeTrip);

        await db.SaveChangesAsync();

        var handler = new StartBookingHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(booking.Id));

        Assert.Equal(
            "The vehicle already has another trip in progress.",
            exception.Message);
    }


    [Fact]
    public async Task Handle_ShouldStartBookingAndRequest_WhenAllConditionsAreValid()
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

        var handler = new StartBookingHandler(db);

        await handler.Handle(booking.Id);

        Assert.Equal(
            BookingStatus.InProgress,
            booking.Status);

        Assert.Equal(
            TransportRequestStatus.InProgress,
            request.Status);
    }


    [Fact]
    public async Task Handle_ShouldNotThrow_WhenVehicleHasPublishedTrip()
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

        var publishedTrip = new Trip(
            transporter.Id,
            vehicle.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(3),
            DateTime.UtcNow.AddDays(3).AddHours(8),
            1500m,
            10m);

        db.Users.Add(user);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);
        db.Trips.Add(publishedTrip);

        await db.SaveChangesAsync();

        var handler = new StartBookingHandler(db);

        await handler.Handle(booking.Id);

        Assert.Equal(
            BookingStatus.InProgress,
            booking.Status);

        Assert.Equal(
            TransportRequestStatus.InProgress,
            request.Status);
    }
}