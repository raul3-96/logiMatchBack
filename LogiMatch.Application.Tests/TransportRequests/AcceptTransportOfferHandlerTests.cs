using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.TransportOffers;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMatch.Application.Tests.TransportOffers;

public class AcceptTransportOfferHandlerTests
{
    [Fact]
    public async Task Handle_WhenOfferDoesNotExist_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new AcceptTransportOfferHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(Guid.NewGuid()));

        Assert.Equal(
            "The specified transport offer does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenOfferIsNotPending_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var offer = db.TransportOffers.Single();
        offer.Accept();

        await db.SaveChangesAsync();

        var handler = new AcceptTransportOfferHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(offer.Id));

        Assert.Equal(
            "Only pending offers can be accepted.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestDoesNotExist_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var transporter = CreateTransporter();
        var vehicle = CreateVehicle(transporter.Id);

        var offer = new TransportOffer(
            Guid.NewGuid(),
            transporter.Id,
            vehicle.Id,
            350m,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(4));

        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportOffers.Add(offer);

        await db.SaveChangesAsync();

        var handler = new AcceptTransportOfferHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(offer.Id));

        Assert.Equal(
            "The transport request associated with the offer does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenVehicleDoesNotExist_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        request.Publish();

        var transporter = CreateTransporter();

        var offer = new TransportOffer(
            request.Id,
            transporter.Id,
            Guid.NewGuid(),
            350m,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(4));

        db.TransportRequests.Add(request);
        db.TransporterProfiles.Add(transporter);
        db.TransportOffers.Add(offer);

        await db.SaveChangesAsync();

        var handler = new AcceptTransportOfferHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(offer.Id));

        Assert.Equal(
            "The vehicle associated with the offer does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenVehicleDoesNotBelongToTransporter_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        request.Publish();

        var transporter = CreateTransporter();
        var otherTransporter = CreateTransporter();

        var vehicle = CreateVehicle(otherTransporter.Id);

        var offer = new TransportOffer(
            request.Id,
            transporter.Id,
            vehicle.Id,
            350m,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(4));

        db.TransportRequests.Add(request);
        db.TransporterProfiles.AddRange(
            transporter,
            otherTransporter);
        db.Vehicles.Add(vehicle);
        db.TransportOffers.Add(offer);

        await db.SaveChangesAsync();

        var handler = new AcceptTransportOfferHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(offer.Id));

        Assert.Equal(
            "The vehicle does not belong to the transporter who created the offer.",
            exception.Message);
    }

    [Theory]
    [InlineData(TransportRequestStatus.Draft)]
    [InlineData(TransportRequestStatus.Published)]
    [InlineData(TransportRequestStatus.Matching)]
    [InlineData(TransportRequestStatus.Accepted)]
    [InlineData(TransportRequestStatus.InProgress)]
    [InlineData(TransportRequestStatus.Completed)]
    [InlineData(TransportRequestStatus.Cancelled)]
    [InlineData(TransportRequestStatus.Expired)]
    public async Task Handle_WhenRequestStatusIsNotOffersReceived_ShouldThrow(
        TransportRequestStatus status)
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        SetRequestStatus(request, status);

        var transporter = CreateTransporter();
        var vehicle = CreateVehicle(transporter.Id);

        var offer = CreateOffer(request, transporter, vehicle);

        db.TransportRequests.Add(request);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportOffers.Add(offer);
        db.VehicleAvailabilities.Add(
            CreateAvailability(vehicle.Id));

        await db.SaveChangesAsync();

        var handler = new AcceptTransportOfferHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(offer.Id));

        Assert.Equal(
            "Only requests with received offers can accept an offer.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestHasActiveBooking_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var offer = db.TransportOffers.Single();
        var request = db.TransportRequests.Single();

        var booking = new Booking(
            request.Id,
            offer.Id);

        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new AcceptTransportOfferHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(offer.Id));

        Assert.Equal(
            "The transport request already has an active booking.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestHasActiveTripCargo_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var request = db.TransportRequests.Single();
        var offer = db.TransportOffers.Single();

        var trip = CreateTrip(
            db.Vehicles.Single().Id);

        var tripCargo = new TripCargo(
            trip.Id,
            request.Id,
            500m,
            2m);

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);

        await db.SaveChangesAsync();

        var handler = new AcceptTransportOfferHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(offer.Id));

        Assert.Equal(
            "The transport request is already assigned to a trip.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenVehicleIsNoLongerAvailable_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var request = db.TransportRequests.Single();
        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();

        var offer = db.TransportOffers.Single();

        db.VehicleAvailabilities.RemoveRange(
            db.VehicleAvailabilities);

        await db.SaveChangesAsync();

        var handler = new AcceptTransportOfferHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(offer.Id));

        Assert.Equal(
            "The vehicle is no longer available for the offer dates.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenVehicleHasOverlappingBooking_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var request = db.TransportRequests.Single();
        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();

        var offer = db.TransportOffers.Single();

        var otherRequestPickup = DateTime.UtcNow.AddDays(1).AddHours(2);

        var otherRequest = CreateRequest(
            otherRequestPickup,
            otherRequestPickup.AddHours(8));

        otherRequest.Publish();
        otherRequest.MarkOffersReceived();

        var otherOffer = new TransportOffer(
            otherRequest.Id,
            transporter.Id,
            vehicle.Id,
            400m,
            otherRequestPickup,
            otherRequestPickup.AddHours(4));

        var otherBooking = new Booking(
            otherRequest.Id,
            otherOffer.Id);

        db.TransportRequests.Add(otherRequest);
        db.TransportOffers.Add(otherOffer);
        db.Bookings.Add(otherBooking);

        await db.SaveChangesAsync();

        var handler = new AcceptTransportOfferHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(offer.Id));

        Assert.Equal(
            "The vehicle is already committed to another booking that overlaps with the specified offer dates.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenVehicleHasOverlappingTrip_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var offer = db.TransportOffers.Single();
        var vehicle = db.Vehicles.Single();
        var transporter = db.TransporterProfiles.Single();

        var trip = new Trip(
            transporter.Id,
            vehicle.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1).AddHours(2),
            DateTime.UtcNow.AddDays(1).AddHours(6),
            1000m,
            5m);

        db.Trips.Add(trip);

        await db.SaveChangesAsync();

        var handler = new AcceptTransportOfferHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(offer.Id));

        Assert.Equal(
            "The vehicle already has a trip that overlaps with the specified offer dates.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenAccepted_ShouldRejectOtherPendingOffers()
    {
        using var db = await CreateValidDatabase();

        var request = db.TransportRequests.Single();
        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();

        var otherTransporter = CreateTransporter();
        var otherVehicle = CreateVehicle(otherTransporter.Id);

        var otherOffer = CreateOffer(
            request,
            otherTransporter,
            otherVehicle);

        db.TransporterProfiles.Add(otherTransporter);
        db.Vehicles.Add(otherVehicle);
        db.TransportOffers.Add(otherOffer);

        await db.SaveChangesAsync();

        var offer = db.TransportOffers
            .First(x => x.TransporterProfileId == transporter.Id);

        var handler = new AcceptTransportOfferHandler(db);

        await handler.Handle(offer.Id);

        var acceptedOffer = await db.TransportOffers
            .SingleAsync(x => x.Id == offer.Id);

        var rejectedOffer = await db.TransportOffers
            .SingleAsync(x => x.Id == otherOffer.Id);

        Assert.Equal(
            TransportOfferStatus.Accepted,
            acceptedOffer.Status);

        Assert.Equal(
            TransportOfferStatus.Rejected,
            rejectedOffer.Status);
    }

    [Fact]
    public async Task Handle_WhenAccepted_ShouldAcceptRequest()
    {
        using var db = await CreateValidDatabase();

        var request = db.TransportRequests.Single();
        var offer = db.TransportOffers.Single();

        var handler = new AcceptTransportOfferHandler(db);

        await handler.Handle(offer.Id);

        Assert.Equal(
            TransportRequestStatus.Accepted,
            request.Status);

        Assert.Equal(
            FulfillmentMode.Offer,
            request.Fulfillment);
    }

    [Fact]
    public async Task Handle_WhenAccepted_ShouldCreateBooking()
    {
        using var db = await CreateValidDatabase();

        var offer = db.TransportOffers.Single();

        var handler = new AcceptTransportOfferHandler(db);

        var bookingId = await handler.Handle(offer.Id);

        var booking = await db.Bookings
            .SingleAsync(x => x.Id == bookingId);

        Assert.Equal(
            offer.TransportRequestId,
            booking.TransportRequestId);

        Assert.Equal(
            offer.Id,
            booking.TransportOfferId);

        Assert.Equal(
            BookingStatus.Confirmed,
            booking.Status);
    }

    [Fact]
    public async Task Handle_WhenAccepted_ShouldReturnBookingId()
    {
        using var db = await CreateValidDatabase();

        var offer = db.TransportOffers.Single();

        var handler = new AcceptTransportOfferHandler(db);

        var bookingId = await handler.Handle(offer.Id);

        Assert.NotEqual(Guid.Empty, bookingId);
    }

    [Fact]
    public async Task Handle_WhenCancelledBookingExists_ShouldAllowAcceptance()
    {
        using var db = await CreateValidDatabase();

        var request = db.TransportRequests.Single();
        var offer = db.TransportOffers.Single();

        var cancelledBooking = new Booking(
            request.Id,
            offer.Id);

        cancelledBooking.Cancel();

        db.Bookings.Add(cancelledBooking);

        await db.SaveChangesAsync();

        var handler = new AcceptTransportOfferHandler(db);

        var bookingId = await handler.Handle(offer.Id);

        Assert.NotEqual(Guid.Empty, bookingId);

        Assert.Equal(
            TransportOfferStatus.Accepted,
            offer.Status);
    }

    [Fact]
    public async Task Handle_WhenCancelledTripCargoExists_ShouldAllowAcceptance()
    {
        using var db = await CreateValidDatabase();

        var request = db.TransportRequests.Single();
        var offer = db.TransportOffers.Single();
        var vehicle = db.Vehicles.Single();

        var trip = new Trip(
            Guid.NewGuid(),
            vehicle.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(5),
            DateTime.UtcNow.AddDays(5).AddHours(8),
            1500m,
            10m);

        var tripCargo = new TripCargo(
            trip.Id,
            request.Id,
            500m,
            2m);

        tripCargo.Cancel();

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);

        await db.SaveChangesAsync();

        var handler = new AcceptTransportOfferHandler(db);

        var bookingId = await handler.Handle(offer.Id);

        Assert.NotEqual(Guid.Empty, bookingId);

        Assert.Equal(
            TransportOfferStatus.Accepted,
            offer.Status);
    }

    private static async Task<TestDbContext> CreateValidDatabase()
    {
        var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        request.Publish();
        request.MarkOffersReceived();

        var transporter = CreateTransporter();
        var vehicle = CreateVehicle(transporter.Id);

        var offer = CreateOffer(
            request,
            transporter,
            vehicle);

        db.TransportRequests.Add(request);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.TransportOffers.Add(offer);

        db.VehicleAvailabilities.Add(
            CreateAvailability(vehicle.Id));

        await db.SaveChangesAsync();

        return db;
    }

    private static TransportRequest CreateRequest(
        DateTime? pickupDate = null,
        DateTime? deliveryDate = null)
    {
        var pickup = pickupDate ?? DateTime.UtcNow.AddDays(1);

        return new TransportRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            pickup,
            deliveryDate ?? pickup.AddHours(8));
    }

    private static TransporterProfile CreateTransporter()
    {
        return new TransporterProfile(Guid.NewGuid());
    }

    private static Vehicle CreateVehicle(Guid transporterId)
    {
        return new Vehicle(
            transporterId,
            VehicleType.Van,
            "Mercedes-Benz",
            "Sprinter",
            Guid.NewGuid().ToString()[..8],
            1500m,
            10m,
            5.5m,
            2m,
            2.5m,
            true,
            false);
    }

    private static TransportOffer CreateOffer(
        TransportRequest request,
        TransporterProfile transporter,
        Vehicle vehicle)
    {
        return new TransportOffer(
            request.Id,
            transporter.Id,
            vehicle.Id,
            350m,
            request.PickupDate,
            request.PickupDate.AddHours(4));
    }

    private static VehicleAvailability CreateAvailability(Guid vehicleId)
    {
        return new VehicleAvailability(
            vehicleId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(10));
    }

    private static Trip CreateTrip(Guid vehicleId)
    {
        return new Trip(
            Guid.NewGuid(),
            vehicleId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(8),
            1500m,
            10m);
    }

    private static void SetRequestStatus(
        TransportRequest request,
        TransportRequestStatus status)
    {
        switch (status)
        {
            case TransportRequestStatus.Draft:
                break;

            case TransportRequestStatus.Published:
                request.Publish();
                break;

            case TransportRequestStatus.Matching:
                request.Publish();
                request.StartMatching();
                break;

            case TransportRequestStatus.OffersReceived:
                request.Publish();
                request.MarkOffersReceived();
                break;

            case TransportRequestStatus.Accepted:
                request.Publish();
                request.MarkOffersReceived();
                request.Accept();
                break;

            case TransportRequestStatus.InProgress:
                request.Publish();
                request.MarkOffersReceived();
                request.Accept();
                request.Start();
                break;

            case TransportRequestStatus.Completed:
                request.Publish();
                request.MarkOffersReceived();
                request.Accept();
                request.Start();
                request.Complete();
                break;

            case TransportRequestStatus.Cancelled:
                request.Cancel();
                break;

            case TransportRequestStatus.Expired:
                request.Publish();
                request.Expire();
                break;
        }
    }
}