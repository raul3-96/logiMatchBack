using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.Trips;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMatch.Application.Tests.Trips;

public class CreateTripHandlerTests
{
    [Fact]
    public async Task Handle_WhenTransporterDoesNotExist_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var vehicle = CreateVehicle(Guid.NewGuid());

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        var command = CreateCommand(
            transporterProfileId: Guid.NewGuid(),
            vehicleId: vehicle.Id);

        var handler = CreateHandler(
            db,
            Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The specified transporter profile does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserDoesNotOwnTransporterProfile_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();
        var locations = db.Locations.ToList();

        var command = new CreateTripCommand
        {
            TransporterProfileId = transporter.Id,
            VehicleId = vehicle.Id,
            OriginLocationId = locations[0].Id,
            DestinationLocationId = locations[1].Id,
            DepartureDate = DateTime.UtcNow.AddDays(1),
            EstimatedArrivalDate = DateTime.UtcNow.AddDays(1).AddHours(8),
            AvailableWeightKg = 1000m,
            AvailableVolumeM3 = 5m
        };

        var handler = CreateHandler(
            db,
            Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The transporter profile does not belong to the current user.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenVehicleDoesNotExist_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();

        var command = CreateCommand(
            transporterProfileId: transporter.Id,
            vehicleId: Guid.NewGuid());

        var handler = CreateHandler(
            db,
            transporter.UserId);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The specified vehicle does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenVehicleBelongsToAnotherTransporter_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var otherTransporter = new TransporterProfile(Guid.NewGuid());
        var vehicle = CreateVehicle(otherTransporter.Id);

        db.TransporterProfiles.Add(otherTransporter);
        db.Vehicles.Add(vehicle);

        await db.SaveChangesAsync();

        var command = CreateCommand(
            transporterProfileId: transporter.Id,
            vehicleId: vehicle.Id);

        var handler = CreateHandler(
            db,
            transporter.UserId);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The vehicle does not belong to the specified transporter.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenOriginDoesNotExist_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();
        var destination = db.Locations.Skip(1).First();

        var command = CreateCommand(
            transporterProfileId: transporter.Id,
            vehicleId: vehicle.Id,
            originLocationId: Guid.NewGuid(),
            destinationLocationId: destination.Id);

        var handler = CreateHandler(
            db,
            transporter.UserId);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The specified origin location does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenDestinationDoesNotExist_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();
        var origin = db.Locations.First();

        var command = CreateCommand(
            transporterProfileId: transporter.Id,
            vehicleId: vehicle.Id,
            originLocationId: origin.Id,
            destinationLocationId: Guid.NewGuid());

        var handler = CreateHandler(
            db,
            transporter.UserId);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The specified destination location does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenOriginAndDestinationAreSame_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();
        var location = db.Locations.First();

        var command = CreateCommand(
            transporterProfileId: transporter.Id,
            vehicleId: vehicle.Id,
            originLocationId: location.Id,
            destinationLocationId: location.Id);

        var handler = CreateHandler(
            db,
            transporter.UserId);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "Origin and destination locations must be different.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenArrivalIsBeforeDeparture_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var departure = DateTime.UtcNow.AddDays(2);
        var arrival = departure.AddHours(-1);

        var command = CreateValidCommand(
            db,
            departureDate: departure,
            estimatedArrivalDate: arrival);

        var handler = CreateHandler(
            db,
            transporter.UserId);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "Estimated arrival must be after departure.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenArrivalEqualsDeparture_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var departure = DateTime.UtcNow.AddDays(2);

        var command = CreateValidCommand(
            db,
            departureDate: departure,
            estimatedArrivalDate: departure);

        var handler = CreateHandler(
            db,
            transporter.UserId);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "Estimated arrival must be after departure.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenDepartureIsInPast_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var departure = DateTime.UtcNow.AddMinutes(-5);
        var arrival = DateTime.UtcNow.AddHours(2);

        var command = CreateValidCommand(
            db,
            departureDate: departure,
            estimatedArrivalDate: arrival);

        var handler = CreateHandler(
            db,
            transporter.UserId);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "Departure date cannot be in the past.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenAvailableWeightExceedsVehicleCapacity_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();

        var command = CreateValidCommand(
            db,
            availableWeightKg: 1501m);

        var handler = CreateHandler(
            db,
            transporter.UserId);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "Available weight cannot exceed vehicle capacity.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenAvailableVolumeExceedsVehicleCapacity_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();

        var command = CreateValidCommand(
            db,
            availableVolumeM3: 10.001m);

        var handler = CreateHandler(
            db,
            transporter.UserId);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(command));

        Assert.Equal(
            "Available volume cannot exceed vehicle capacity.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenVehicleIsNotAvailable_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var departure = DateTime.UtcNow.AddDays(20);
        var arrival = departure.AddHours(8);

        var command = CreateValidCommand(
            db,
            departureDate: departure,
            estimatedArrivalDate: arrival);

        var handler = CreateHandler(
            db,
            transporter.UserId);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The vehicle is not available for the trip dates.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenVehicleHasOverlappingTrip_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();
        var locations = db.Locations.ToList();

        var existingDeparture = DateTime.UtcNow.AddDays(2);
        var existingArrival = existingDeparture.AddHours(8);

        var existingTrip = new Trip(
            transporter.Id,
            vehicle.Id,
            locations[0].Id,
            locations[1].Id,
            existingDeparture,
            existingArrival,
            1000m,
            5m);

        db.Trips.Add(existingTrip);
        await db.SaveChangesAsync();

        var command = CreateValidCommand(
            db,
            departureDate: existingDeparture.AddHours(2),
            estimatedArrivalDate: existingDeparture.AddHours(6));

        var handler = CreateHandler(
            db,
            transporter.UserId);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The vehicle already has a trip that overlaps with the specified dates.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenCancelledTripOverlaps_ShouldAllowCreation()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();
        var locations = db.Locations.ToList();

        var existingDeparture = DateTime.UtcNow.AddDays(2);
        var existingArrival = existingDeparture.AddHours(8);

        var existingTrip = new Trip(
            transporter.Id,
            vehicle.Id,
            locations[0].Id,
            locations[1].Id,
            existingDeparture,
            existingArrival,
            1000m,
            5m);

        existingTrip.Cancel();

        db.Trips.Add(existingTrip);
        await db.SaveChangesAsync();

        var command = CreateValidCommand(
            db,
            departureDate: existingDeparture.AddHours(2),
            estimatedArrivalDate: existingDeparture.AddHours(6));

        var handler = CreateHandler(
            db,
            transporter.UserId);

        var tripId = await handler.Handle(command);

        Assert.NotEqual(Guid.Empty, tripId);
    }

    [Fact]
    public async Task Handle_WhenVehicleHasOverlappingBooking_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();
        var locations = db.Locations.ToList();

        var requestPickup = DateTime.UtcNow.AddDays(2);
        var requestDelivery = requestPickup.AddHours(8);

        var request = new TransportRequest(
            Guid.NewGuid(),
            locations[0].Id,
            locations[1].Id,
            requestPickup,
            requestDelivery);

        request.Publish();
        request.MarkOffersReceived();

        var offer = new TransportOffer(
            request.Id,
            transporter.Id,
            vehicle.Id,
            350m,
            requestPickup,
            requestDelivery);

        offer.Accept();

        var booking = new Booking(
            request.Id,
            offer.Id);

        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var command = CreateValidCommand(
            db,
            departureDate: requestPickup.AddHours(2),
            estimatedArrivalDate: requestPickup.AddHours(6));

        var handler = CreateHandler(
            db,
            transporter.UserId);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(command));

        Assert.Equal(
            "The vehicle already has a booking that overlaps with the specified dates.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenBookingIsCancelled_ShouldAllowCreation()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();
        var locations = db.Locations.ToList();

        var requestPickup = DateTime.UtcNow.AddDays(2);
        var requestDelivery = requestPickup.AddHours(8);

        var request = new TransportRequest(
            Guid.NewGuid(),
            locations[0].Id,
            locations[1].Id,
            requestPickup,
            requestDelivery);

        request.Publish();
        request.MarkOffersReceived();

        var offer = new TransportOffer(
            request.Id,
            transporter.Id,
            vehicle.Id,
            350m,
            requestPickup,
            requestDelivery);

        offer.Accept();

        var booking = new Booking(
            request.Id,
            offer.Id);

        booking.Cancel();

        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        db.Bookings.Add(booking);

        await db.SaveChangesAsync();

        var command = CreateValidCommand(
            db,
            departureDate: requestPickup.AddHours(2),
            estimatedArrivalDate: requestPickup.AddHours(6));

        var handler = CreateHandler(
            db,
            transporter.UserId);

        var tripId = await handler.Handle(command);

        Assert.NotEqual(Guid.Empty, tripId);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldCreateTrip()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var command = CreateValidCommand(db);

        var handler = CreateHandler(
            db,
            transporter.UserId);

        var tripId = await handler.Handle(command);

        Assert.NotEqual(Guid.Empty, tripId);

        var trip = await db.Trips
            .FirstOrDefaultAsync(x => x.Id == tripId);

        Assert.NotNull(trip);

        Assert.Equal(
            command.TransporterProfileId,
            trip!.TransporterProfileId);

        Assert.Equal(
            command.VehicleId,
            trip.VehicleId);

        Assert.Equal(
            command.OriginLocationId,
            trip.OriginLocationId);

        Assert.Equal(
            command.DestinationLocationId,
            trip.DestinationLocationId);

        Assert.Equal(
            command.DepartureDate,
            trip.DepartureDate);

        Assert.Equal(
            command.EstimatedArrivalDate,
            trip.EstimatedArrivalDate);

        Assert.Equal(
            command.AvailableWeightKg,
            trip.AvailableWeightKg);

        Assert.Equal(
            command.AvailableVolumeM3,
            trip.AvailableVolumeM3);

        Assert.Equal(
            command.AvailableWeightKg,
            trip.InitialAvailableWeightKg);

        Assert.Equal(
            command.AvailableVolumeM3,
            trip.InitialAvailableVolumeM3);

        Assert.Equal(
            TripStatus.Published,
            trip.Status);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldPersistOnlyOneTrip()
    {
        using var db = await CreateValidDatabase();

        var transporter = db.TransporterProfiles.Single();
        var command = CreateValidCommand(db);

        var handler = CreateHandler(
            db,
            transporter.UserId);

        await handler.Handle(command);

        Assert.Equal(
            1,
            await db.Trips.CountAsync());
    }

    private static async Task<TestDbContext> CreateValidDatabase()
    {
        var db = TestDbContextFactory.Create();

        var transporter = CreateTransporter();
        var vehicle = CreateVehicle(transporter.Id);

        var origin = CreateLocation();
        var destination = CreateLocation();

        var availability = new VehicleAvailability(
            vehicle.Id,
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddDays(10));

        db.TransporterProfiles.Add(transporter);
        db.Vehicles.Add(vehicle);
        db.Locations.AddRange(origin, destination);
        db.VehicleAvailabilities.Add(availability);

        await db.SaveChangesAsync();

        return db;
    }

    private static CreateTripCommand CreateValidCommand(
        TestDbContext db,
        DateTime? departureDate = null,
        DateTime? estimatedArrivalDate = null,
        decimal availableWeightKg = 1000m,
        decimal availableVolumeM3 = 5m)
    {
        var transporter = db.TransporterProfiles.Single();
        var vehicle = db.Vehicles.Single();
        var locations = db.Locations.ToList();

        var departure =
            departureDate ?? DateTime.UtcNow.AddDays(1);

        var arrival =
            estimatedArrivalDate ?? departure.AddHours(8);

        return new CreateTripCommand
        {
            TransporterProfileId = transporter.Id,
            VehicleId = vehicle.Id,
            OriginLocationId = locations[0].Id,
            DestinationLocationId = locations[1].Id,
            DepartureDate = departure,
            EstimatedArrivalDate = arrival,
            AvailableWeightKg = availableWeightKg,
            AvailableVolumeM3 = availableVolumeM3
        };
    }

    private static CreateTripCommand CreateCommand(
        Guid? transporterProfileId = null,
        Guid? vehicleId = null,
        Guid? originLocationId = null,
        Guid? destinationLocationId = null,
        DateTime? departureDate = null,
        DateTime? estimatedArrivalDate = null,
        decimal availableWeightKg = 1000m,
        decimal availableVolumeM3 = 5m)
    {
        var departure =
            departureDate ?? DateTime.UtcNow.AddDays(1);

        var arrival =
            estimatedArrivalDate ?? departure.AddHours(8);

        return new CreateTripCommand
        {
            TransporterProfileId =
                transporterProfileId ?? Guid.NewGuid(),

            VehicleId =
                vehicleId ?? Guid.NewGuid(),

            OriginLocationId =
                originLocationId ?? Guid.NewGuid(),

            DestinationLocationId =
                destinationLocationId ?? Guid.NewGuid(),

            DepartureDate = departure,

            EstimatedArrivalDate = arrival,

            AvailableWeightKg =
                availableWeightKg,

            AvailableVolumeM3 =
                availableVolumeM3
        };
    }

    private static CreateTripHandler CreateHandler(
        IApplicationDbContext db,
        Guid currentUserId)
    {
        return new CreateTripHandler(
            db,
            new FakeCurrentUserService(currentUserId));
    }

    private static TransporterProfile CreateTransporter()
    {
        return new TransporterProfile(Guid.NewGuid());
    }

    private static Vehicle CreateVehicle(Guid transporterProfileId)
    {
        return new Vehicle(
            transporterProfileId,
            VehicleType.Van,
            "Mercedes-Benz",
            "Sprinter",
            "1234ABC",
            1500m,
            10m,
            5.5m,
            2m,
            2.5m,
            true,
            false);
    }

    private static Location CreateLocation()
    {
        return new Location(
            "Test address",
            "Sevilla",
            "41001",
            "Spain",
            37.3772,
            -5.9869);
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