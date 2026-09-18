using LogiMatch.Application.Cargos;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMatch.Application.Tests.Cargos;

public class CreateCargoHandlerTests
{
    [Fact]
    public async Task Handle_WhenRequestDoesNotExist_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new CreateCargoHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand()));

        Assert.Equal(
            "The specified transport request does not exist.",
            exception.Message);
    }

    [Theory]
    [InlineData(TransportRequestStatus.InProgress)]
    [InlineData(TransportRequestStatus.Completed)]
    [InlineData(TransportRequestStatus.Cancelled)]
    [InlineData(TransportRequestStatus.Expired)]
    public async Task Handle_WhenRequestStatusDoesNotAllowCargo_ShouldThrow(
        TransportRequestStatus status)
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        SetRequestStatus(request, status);

        db.TransportRequests.Add(request);
        await db.SaveChangesAsync();

        var handler = new CreateCargoHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(request.Id)));

        Assert.Equal(
            "Cargo can only be added to draft or published transport requests.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestIsAlreadyAssignedToTrip_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        request.Publish();

        var trip = CreateTrip();
        var tripCargo = new TripCargo(
            trip.Id,
            request.Id,
            100m,
            1m);

        db.TransportRequests.Add(request);
        db.Trips.Add(trip);
        db.TripCargos.Add(tripCargo);

        await db.SaveChangesAsync();

        var handler = new CreateCargoHandler(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(request.Id)));

        Assert.Equal(
            "Cargo cannot be added because the transport request is already assigned to a trip.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestIsDraft_ShouldCreateCargo()
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();

        db.TransportRequests.Add(request);
        await db.SaveChangesAsync();

        var handler = new CreateCargoHandler(db);

        var command = CreateCommand(
            request.Id,
            description: "Pallets de mercancía",
            weightKg: 850m,
            volumeM3: 4.5m,
            quantity: 10,
            requiresRefrigeration: false,
            requiresTailLift: true);

        var cargoId = await handler.Handle(command);

        var cargo = await db.Cargos
            .SingleAsync(x => x.Id == cargoId);

        Assert.NotEqual(Guid.Empty, cargoId);
        Assert.Equal(request.Id, cargo.TransportRequestId);
        Assert.Equal("Pallets de mercancía", cargo.Description);
        Assert.Equal(850m, cargo.WeightKg);
        Assert.Equal(4.5m, cargo.VolumeM3);
        Assert.Equal(10, cargo.Quantity);
        Assert.False(cargo.RequiresRefrigeration);
        Assert.True(cargo.RequiresTailLift);
    }

    [Fact]
    public async Task Handle_WhenRequestIsPublished_ShouldCreateCargo()
    {
        using var db = TestDbContextFactory.Create();

        var request = CreateRequest();
        request.Publish();

        db.TransportRequests.Add(request);
        await db.SaveChangesAsync();

        var handler = new CreateCargoHandler(db);

        var cargoId = await handler.Handle(
            CreateCommand(request.Id));

        var cargo = await db.Cargos
            .SingleAsync(x => x.Id == cargoId);

        Assert.NotEqual(Guid.Empty, cargoId);
        Assert.Equal(request.Id, cargo.TransportRequestId);
        Assert.Equal("Test cargo", cargo.Description);
        Assert.Equal(850m, cargo.WeightKg);
        Assert.Equal(4.5m, cargo.VolumeM3);
        Assert.Equal(10, cargo.Quantity);
        Assert.False(cargo.RequiresRefrigeration);
        Assert.False(cargo.RequiresTailLift);
    }

    private static CreateCargoCommand CreateCommand(
        Guid? requestId = null,
        string description = "Test cargo",
        decimal weightKg = 850m,
        decimal volumeM3 = 4.5m,
        int quantity = 10,
        bool requiresRefrigeration = false,
        bool requiresTailLift = false)
    {
        return new CreateCargoCommand
        {
            TransportRequestId = requestId ?? Guid.NewGuid(),
            Description = description,
            WeightKg = weightKg,
            VolumeM3 = volumeM3,
            Quantity = quantity,
            RequiresRefrigeration = requiresRefrigeration,
            RequiresTailLift = requiresTailLift
        };
    }

    private static TransportRequest CreateRequest()
    {
        return new TransportRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(8));
    }

    private static Trip CreateTrip()
    {
        return new Trip(
            Guid.NewGuid(),
            Guid.NewGuid(),
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