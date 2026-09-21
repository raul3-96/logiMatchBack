using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.TransportRequests;
using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMatch.Application.Tests.TransportRequests;

public class CreateTransportRequestHandlerTests
{
    [Fact]
    public async Task Handle_WhenCustomerDoesNotExist_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateTransportRequestHandler(db,new MockCurrentUserService(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand()));

        Assert.Equal(
            "The specified customer does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenCustomerIsNotActive_ShouldThrow()
    {
        using var db = await CreateValidDatabase();

        var customer = db.Users.Single();
        SetUserStatus(customer, UserStatus.Suspended);
        await db.SaveChangesAsync();

        var handler = new CreateTransportRequestHandler(db,new MockCurrentUserService(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(
                customerId: customer.Id,
                pickupLocationId: db.Locations.First().Id,
                deliveryLocationId: db.Locations.Last().Id)));

        Assert.Equal(
            "The specified customer is not active.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenPickupLocationDoesNotExist_ShouldThrow()
    {
        using var db = await CreateValidDatabase();
        var customer = db.Users.Single();

        var handler = new CreateTransportRequestHandler(db,new MockCurrentUserService(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(
                customerId: customer.Id,
                pickupLocationId: Guid.NewGuid(),
                deliveryLocationId: db.Locations.Last().Id)));

        Assert.Equal(
            "The specified pickup location does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenDeliveryLocationDoesNotExist_ShouldThrow()
    {
        using var db = await CreateValidDatabase();
        var customer = db.Users.Single();
        var pickupLocation = db.Locations.First();

        var handler = new CreateTransportRequestHandler(db,new MockCurrentUserService(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(
                customerId: customer.Id,
                pickupLocationId: pickupLocation.Id,
                deliveryLocationId: Guid.NewGuid())));

        Assert.Equal(
            "The specified delivery location does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenPickupAndDeliveryAreSame_ShouldThrow()
    {
        using var db = await CreateValidDatabase();
        var customer = db.Users.Single();
        var location = db.Locations.First();

        var handler = new CreateTransportRequestHandler(db,new MockCurrentUserService(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(
                customerId: customer.Id,
                pickupLocationId: location.Id,
                deliveryLocationId: location.Id)));

        Assert.Equal(
            "Pickup and delivery locations must be different.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenPickupDateIsInThePast_ShouldThrow()
    {
        using var db = await CreateValidDatabase();
        var customer = db.Users.Single();
        var pickupLocation = db.Locations.First();
        var deliveryLocation = db.Locations.Last();

        var handler = new CreateTransportRequestHandler(db,new MockCurrentUserService(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(
                customerId: customer.Id,
                pickupLocationId: pickupLocation.Id,
                deliveryLocationId: deliveryLocation.Id,
                pickupDate: DateTime.UtcNow.AddMinutes(-5),
                deliveryDate: DateTime.UtcNow.AddHours(2))));

        Assert.Equal(
            "Pickup date cannot be in the past.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenDeliveryDateIsBeforePickupDate_ShouldThrow()
    {
        using var db = await CreateValidDatabase();
        var customer = db.Users.Single();
        var pickupLocation = db.Locations.First();
        var deliveryLocation = db.Locations.Last();

        var pickupDate = DateTime.UtcNow.AddDays(1);
        var deliveryDate = pickupDate.AddHours(-1);

        var handler = new CreateTransportRequestHandler(db,new MockCurrentUserService(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(
                customerId: customer.Id,
                pickupLocationId: pickupLocation.Id,
                deliveryLocationId: deliveryLocation.Id,
                pickupDate: pickupDate,
                deliveryDate: deliveryDate)));

        Assert.Equal(
            "Delivery date cannot be before pickup date.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestIsValid_ShouldCreateTransportRequest()
    {
        using var db = await CreateValidDatabase();
        var customer = db.Users.Single();
        var pickupLocation = db.Locations.First();
        var deliveryLocation = db.Locations.Last();

        var pickupDate = DateTime.UtcNow.AddDays(1);
        var deliveryDate = pickupDate.AddHours(8);

        var handler = new CreateTransportRequestHandler(db,new MockCurrentUserService(Guid.NewGuid()));

        var requestId = await handler.Handle(CreateCommand(
            customerId: customer.Id,
            pickupLocationId: pickupLocation.Id,
            deliveryLocationId: deliveryLocation.Id,
            pickupDate: pickupDate,
            deliveryDate: deliveryDate));

        var request = await db.TransportRequests
            .SingleAsync(x => x.Id == requestId);

        Assert.NotEqual(Guid.Empty, requestId);
        Assert.Equal(customer.Id, request.CustomerId);
        Assert.Equal(pickupLocation.Id, request.PickupLocationId);
        Assert.Equal(deliveryLocation.Id, request.DeliveryLocationId);
        Assert.Equal(pickupDate, request.PickupDate);
        Assert.Equal(deliveryDate, request.DeliveryDate);
        Assert.Equal(TransportRequestStatus.Draft, request.Status);
    }

    private static async Task<TestDbContext> CreateValidDatabase()
    {
        var db = TestDbContextFactory.Create();

        db.Users.Add(CreateUser());
        db.Locations.Add(CreateLocation());
        db.Locations.Add(CreateLocation(
            address: "Calle Alcalá 1",
            city: "Madrid",
            postalCode: "28014",
            country: "Spain",
            latitude: 40.4195,
            longitude: -3.6920));

        await db.SaveChangesAsync();

        return db;
    }

    private static CreateTransportRequestCommand CreateCommand(
        Guid? customerId = null,
        Guid? pickupLocationId = null,
        Guid? deliveryLocationId = null,
        DateTime? pickupDate = null,
        DateTime? deliveryDate = null)
    {
        var defaultPickupDate = DateTime.UtcNow.AddDays(1);

        return new CreateTransportRequestCommand
        {
            CustomerId = customerId ?? Guid.NewGuid(),
            PickupLocationId = pickupLocationId ?? Guid.NewGuid(),
            DeliveryLocationId = deliveryLocationId ?? Guid.NewGuid(),
            PickupDate = pickupDate ?? defaultPickupDate,
            DeliveryDate = deliveryDate ?? defaultPickupDate.AddHours(8)
        };
    }

    private static User CreateUser()
    {
        return new User(
            "customer@example.com",
            "John",
            "Doe",
            "600123456");
    }

    private static Location CreateLocation(
        string address = "Plaza de España 1",
        string city = "Sevilla",
        string postalCode = "41013",
        string country = "Spain",
        double latitude = 37.3772,
        double longitude = -5.9869)
    {
        return new Location(
            address,
            city,
            postalCode,
            country,
            latitude,
            longitude);
    }

    private static void SetUserStatus(
        User user,
        UserStatus status)
    {
        var property = typeof(User).GetProperty(nameof(User.Status));
        var setter = property?.GetSetMethod(true);

        if (setter == null)
            throw new InvalidOperationException(
                "Unable to set user status in test.");

        setter.Invoke(user, new object[] { status });
    }
}