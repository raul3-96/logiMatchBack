using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.TransportOffers;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMatch.Application.Tests.TransportOffers;

public class CreateTransportOfferHandlerTests
{
    [Fact]
    public async Task Handle_WhenRequestDoesNotExist_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The specified transport request does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenTransporterDoesNotExist_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        request.Publish();

        db.TransportRequests.Add(request);
        db.Cargos.Add(CreateCargo(request.Id));
        await db.SaveChangesAsync();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            Guid.NewGuid(),
            Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The specified transporter profile does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenVehicleDoesNotExist_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        request.Publish();

        var transporter = CreateTransporter();

        db.TransportRequests.Add(request);
        db.TransporterProfiles.Add(transporter);
        db.Cargos.Add(CreateCargo(request.Id));

        await db.SaveChangesAsync();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            transporter.Id,
            Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The specified vehicle does not exist.",
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

        db.TransportRequests.Add(request);
        db.TransporterProfiles.AddRange(transporter, otherTransporter);
        db.Vehicles.Add(vehicle);
        db.Cargos.Add(CreateCargo(request.Id));

        await db.SaveChangesAsync();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The vehicle does not belong to the specified transporter.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenPriceIsZero_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            db.TransportRequests.Single().Id,
            db.TransporterProfiles.Single().Id,
            db.Vehicles.Single().Id,
            price: 0);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The offer price must be greater than zero.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenPickupDateIsInThePast_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            db.TransportRequests.Single().Id,
            db.TransporterProfiles.Single().Id,
            db.Vehicles.Single().Id,
            pickup: DateTime.UtcNow.AddMinutes(-5),
            delivery: DateTime.UtcNow.AddHours(2));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "Estimated pickup date cannot be in the past.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenDeliveryIsBeforePickup_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var pickup = DateTime.UtcNow.AddDays(1);
        var delivery = pickup.AddHours(-1);

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            db.TransportRequests.Single().Id,
            db.TransporterProfiles.Single().Id,
            db.Vehicles.Single().Id,
            pickup: pickup,
            delivery: delivery);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "Estimated delivery date must be after estimated pickup date.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenPickupIsBeforeRequestPickup_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var requestPickup = DateTime.UtcNow.AddDays(2);

        var request = CreateRequest(
            pickupDate: requestPickup,
            deliveryDate: requestPickup.AddHours(8));

        request.Publish();

        var transporter = CreateTransporter();
        var vehicle = CreateVehicle(transporter.Id);

        db.TransportRequests.Add(request);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Cargos.Add(CreateCargo(request.Id));
        db.VehicleAvailabilities.Add(
            CreateAvailability(vehicle.Id));

        await db.SaveChangesAsync();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            transporter.Id,
            vehicle.Id,
            pickup: DateTime.UtcNow.AddDays(1),
            delivery: DateTime.UtcNow.AddDays(1).AddHours(2));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "Estimated pickup date cannot be before the requested pickup date.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenDeliveryIsAfterRequestDeadline_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest(
            pickupDate: DateTime.UtcNow.AddDays(1),
            deliveryDate: DateTime.UtcNow.AddDays(1).AddHours(4));

        request.Publish();

        var transporter = CreateTransporter();
        var vehicle = CreateVehicle(transporter.Id);

        db.TransportRequests.Add(request);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Cargos.Add(CreateCargo(request.Id));
        db.VehicleAvailabilities.Add(
            CreateAvailability(vehicle.Id));

        await db.SaveChangesAsync();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            transporter.Id,
            vehicle.Id,
            pickup: DateTime.UtcNow.AddDays(1).AddHours(1),
            delivery: DateTime.UtcNow.AddDays(1).AddHours(5));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "Estimated delivery date cannot be after the requested delivery date.",
            exception.Message);
    }

    [Theory]
    [InlineData(TransportRequestStatus.Draft)]
    [InlineData(TransportRequestStatus.Accepted)]
    [InlineData(TransportRequestStatus.InProgress)]
    [InlineData(TransportRequestStatus.Completed)]
    [InlineData(TransportRequestStatus.Cancelled)]
    [InlineData(TransportRequestStatus.Expired)]
    public async Task Handle_WhenRequestStatusDoesNotAllowOffers_ShouldThrow(
        TransportRequestStatus status)
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();

        SetRequestStatus(request, status);

        var transporter = CreateTransporter();
        var vehicle = CreateVehicle(transporter.Id);

        db.TransportRequests.Add(request);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Cargos.Add(CreateCargo(request.Id));
        db.VehicleAvailabilities.Add(
            CreateAvailability(vehicle.Id));

        await db.SaveChangesAsync();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "Offers cannot be created for the current transport request status.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestHasActiveTripCargo_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var request = db.TransportRequests.Single();
        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();

        var trip = CreateTrip(vehicle.Id);

        var tripCargo = new TripCargo(
            trip.Id,
            request.Id,
            500m,
            2m);

        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);

        await db.SaveChangesAsync();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The transport request is already assigned to a trip.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestHasNoCargo_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        request.Publish();

        var transporter = CreateTransporter();
        var vehicle = CreateVehicle(transporter.Id);

        db.TransportRequests.Add(request);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.VehicleAvailabilities.Add(
            CreateAvailability(vehicle.Id));

        await db.SaveChangesAsync();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The transport request has no cargo.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenCargoExceedsVehicleWeight_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        request.Publish();

        var transporter = CreateTransporter();
        var vehicle = CreateVehicle(
            transporter.Id,
            maxWeightKg: 500m);

        db.TransportRequests.Add(request);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Cargos.Add(
            CreateCargo(request.Id, weightKg: 850m));
        db.VehicleAvailabilities.Add(
            CreateAvailability(vehicle.Id));

        await db.SaveChangesAsync();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The vehicle does not have enough weight capacity for the transport request.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenCargoExceedsVehicleVolume_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        request.Publish();

        var transporter = CreateTransporter();
        var vehicle = CreateVehicle(
            transporter.Id,
            maxVolumeM3: 2m);

        db.TransportRequests.Add(request);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Cargos.Add(
            CreateCargo(request.Id, volumeM3: 4.5m));
        db.VehicleAvailabilities.Add(
            CreateAvailability(vehicle.Id));

        await db.SaveChangesAsync();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The vehicle does not have enough volume capacity for the transport request.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRefrigerationIsRequired_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        request.Publish();

        var transporter = CreateTransporter();
        var vehicle = CreateVehicle(
            transporter.Id,
            isRefrigerated: false);

        db.TransportRequests.Add(request);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Cargos.Add(
            CreateCargo(
                request.Id,
                requiresRefrigeration: true));
        db.VehicleAvailabilities.Add(
            CreateAvailability(vehicle.Id));

        await db.SaveChangesAsync();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The vehicle does not meet the refrigeration requirement.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenTailLiftIsRequired_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        request.Publish();

        var transporter = CreateTransporter();
        var vehicle = CreateVehicle(
            transporter.Id,
            hasTailLift: false);

        db.TransportRequests.Add(request);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Cargos.Add(
            CreateCargo(
                request.Id,
                requiresTailLift: true));
        db.VehicleAvailabilities.Add(
            CreateAvailability(vehicle.Id));

        await db.SaveChangesAsync();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The vehicle does not meet the tail lift requirement.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenVehicleIsNotAvailable_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        request.Publish();

        var transporter = CreateTransporter();
        var vehicle = CreateVehicle(transporter.Id);

        db.TransportRequests.Add(request);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Cargos.Add(CreateCargo(request.Id));

        db.VehicleAvailabilities.Add(
            new VehicleAvailability(
                vehicle.Id,
                DateTime.UtcNow.AddDays(5),
                DateTime.UtcNow.AddDays(6)));

        await db.SaveChangesAsync();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The vehicle is not available for the estimated offer dates.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenVehicleHasOverlappingTrip_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var request = db.TransportRequests.Single();
        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();

        db.Trips.Add(
            new Trip(
                transporter.Id,
                vehicle.Id,
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow.AddDays(1).AddHours(2),
                DateTime.UtcNow.AddDays(1).AddHours(6),
                1000m,
                5m));

        await db.SaveChangesAsync();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The vehicle already has a trip that overlaps with the specified offer dates.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenPendingOfferAlreadyExists_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var request = db.TransportRequests.Single();
        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();

        db.TransportOffers.Add(
            new TransportOffer(
                request.Id,
                transporter.Id,
                vehicle.Id,
                350m,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(1).AddHours(4)));

        await db.SaveChangesAsync();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The transporter already has a pending offer for this transport request.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenAcceptedOfferHasActiveBooking_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        request.Publish();

        var transporter = CreateTransporter();
        var vehicle = CreateVehicle(
            transporter.Id,
            hasTailLift: true,
            isRefrigerated: true);

        var cargo = CreateCargo(
            request.Id,
            requiresTailLift: false,
            requiresRefrigeration: false);

        var availability = CreateAvailability(vehicle.Id);

        var acceptedOffer = new TransportOffer(
            request.Id,
            transporter.Id,
            vehicle.Id,
            350m,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(4));

        acceptedOffer.Accept();

        var booking = new Booking(
            request.Id,
            acceptedOffer.Id);

        db.TransportRequests.Add(request);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Cargos.Add(cargo);
        db.VehicleAvailabilities.Add(availability);
        db.TransportOffers.Add(acceptedOffer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The transporter already has an active booking for this transport request.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestIsPublished_ShouldCreateOfferAndMoveToOffersReceived()
    {
        using var db = await CreateValidDatabase();

        var request = db.TransportRequests.Single();
        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();

        var handler = new CreateTransportOfferHandler(db);

        var command = CreateCommand(
            request.Id,
            transporter.Id,
            vehicle.Id);

        var offerId = await handler.Handle(command);

        var offer = await db.TransportOffers
            .SingleAsync(x => x.Id == offerId);

        Assert.Equal(
            TransportOfferStatus.Pending,
            offer.Status);

        Assert.Equal(
            TransportRequestStatus.OffersReceived,
            request.Status);
    }

    [Fact]
    public async Task Handle_WhenRequestIsMatching_ShouldCreateOfferAndMoveToOffersReceived()
    {
        using var db = await CreateValidDatabase();

        var request = db.TransportRequests.Single();
        request.StartMatching();

        await db.SaveChangesAsync();

        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();

        var handler = new CreateTransportOfferHandler(db);

        var offerId = await handler.Handle(
            CreateCommand(
                request.Id,
                transporter.Id,
                vehicle.Id));

        var offer = await db.TransportOffers
            .SingleAsync(x => x.Id == offerId);

        Assert.Equal(
            TransportOfferStatus.Pending,
            offer.Status);

        Assert.Equal(
            TransportRequestStatus.OffersReceived,
            request.Status);
    }

    [Fact]
    public async Task Handle_WhenRequestIsOffersReceived_ShouldCreateOfferWithoutChangingStatus()
    {
        using var db = await CreateValidDatabase();

        var request = db.TransportRequests.Single();
        request.MarkOffersReceived();

        await db.SaveChangesAsync();

        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();

        var handler = new CreateTransportOfferHandler(db);

        var offerId = await handler.Handle(
            CreateCommand(
                request.Id,
                transporter.Id,
                vehicle.Id));

        var offer = await db.TransportOffers
            .SingleAsync(x => x.Id == offerId);

        Assert.Equal(
            TransportOfferStatus.Pending,
            offer.Status);

        Assert.Equal(
            TransportRequestStatus.OffersReceived,
            request.Status);
    }

    private static async Task<TestDbContext> CreateValidDatabase()
    {
        var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        request.Publish();

        var transporter = CreateTransporter();
        var vehicle = CreateVehicle(transporter.Id);

        db.TransportRequests.Add(request);
        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);

        db.Cargos.Add(
            CreateCargo(
                request.Id,
                weightKg: 850m,
                volumeM3: 4.5m,
                requiresTailLift: true));

        db.VehicleAvailabilities.Add(
            CreateAvailability(vehicle.Id));

        await db.SaveChangesAsync();

        return db;
    }

    private static CreateTransportOfferCommand CreateCommand(
        Guid? requestId = null,
        Guid? transporterId = null,
        Guid? vehicleId = null,
        decimal price = 350m,
        DateTime? pickup = null,
        DateTime? delivery = null)
    {
        var defaultPickup = DateTime.UtcNow.AddDays(1);
        var defaultDelivery = defaultPickup.AddHours(4);

        return new CreateTransportOfferCommand
        {
            TransportRequestId = requestId ?? Guid.NewGuid(),
            TransporterProfileId = transporterId ?? Guid.NewGuid(),
            VehicleId = vehicleId ?? Guid.NewGuid(),
            Price = price,
            EstimatedPickupDate = pickup ?? defaultPickup,
            EstimatedDeliveryDate = delivery ?? defaultDelivery
        };
    }

    private static TransportRequest CreateRequest(
        DateTime? pickupDate = null,
        DateTime? deliveryDate = null)
    {
        return new TransportRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            pickupDate ?? DateTime.UtcNow.AddDays(1),
            deliveryDate ?? DateTime.UtcNow.AddDays(1).AddHours(8));
    }

    private static TransporterProfile CreateTransporter()
    {
        return new TransporterProfile(Guid.NewGuid());
    }

    private static Vehicle CreateVehicle(
        Guid transporterId,
        decimal maxWeightKg = 1500m,
        decimal maxVolumeM3 = 10m,
        bool hasTailLift = true,
        bool isRefrigerated = false)
    {
        return new Vehicle(
            transporterId,
            VehicleType.Van,
            "Mercedes-Benz",
            "Sprinter",
            Guid.NewGuid().ToString()[..8],
            maxWeightKg,
            maxVolumeM3,
            5.5m,
            2m,
            2.5m,
            hasTailLift,
            isRefrigerated);
    }

    private static Cargo CreateCargo(
    Guid requestId,
    decimal weightKg = 850m,
    decimal volumeM3 = 4.5m,
    bool requiresRefrigeration = false,
    bool requiresTailLift = false)
    {
        return new Cargo(
            requestId,
            "Test cargo",
            weightKg,
            volumeM3,
            10,
            requiresRefrigeration,
            requiresTailLift);
    }

    private static VehicleAvailability CreateAvailability(Guid vehicleId)
    {
        return new VehicleAvailability(
            vehicleId,
            DateTime.UtcNow.AddHours(1),
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