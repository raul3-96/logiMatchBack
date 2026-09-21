using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.TransportOffers;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Xunit;

namespace LogiMatch.Application.Tests.TransportOffers;

public class RejectTransportOfferHandlerTests
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenOfferDoesNotExist()
    {
        using var db = TestDbContextFactory.Create();

        var handler = new RejectTransportOfferHandler(
            db,
            new MockCurrentUserService(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(Guid.NewGuid()));

        Assert.Equal(
            "The specified transport offer does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTransportRequestDoesNotExist()
    {
        using var db = TestDbContextFactory.Create();

        var offer = new TransportOffer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            350m,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));

        db.TransportOffers.Add(offer);
        await db.SaveChangesAsync();

        var handler = new RejectTransportOfferHandler(
            db,
            new MockCurrentUserService(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(offer.Id));

        Assert.Equal(
            "The transport request associated with the offer does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenCurrentUserIsNotRequestOwner()
    {
        using var db = TestDbContextFactory.Create();

        var customerId = Guid.NewGuid();

        var request = new TransportRequest(
            customerId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));

        request.Publish();
        request.MarkOffersReceived();

        var offer = new TransportOffer(
            request.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            350m,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));

        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        await db.SaveChangesAsync();

        var handler = new RejectTransportOfferHandler(
            db,
            new MockCurrentUserService(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(offer.Id));

        Assert.Equal(
            "Only the owner of the transport request can reject offers.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOfferIsNotPending()
    {
        using var db = TestDbContextFactory.Create();

        var customerId = Guid.NewGuid();

        var request = new TransportRequest(
            customerId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));

        request.Publish();
        request.MarkOffersReceived();

        var offer = new TransportOffer(
            request.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            350m,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));

        offer.Reject();

        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        await db.SaveChangesAsync();

        var handler = new RejectTransportOfferHandler(
            db,
            new MockCurrentUserService(customerId));

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(offer.Id));

        Assert.Equal(
            "Only pending offers can be rejected.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldRejectOffer_WhenCurrentUserOwnsRequest()
    {
        using var db = TestDbContextFactory.Create();

        var customerId = Guid.NewGuid();

        var request = new TransportRequest(
            customerId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));

        request.Publish();
        request.MarkOffersReceived();

        var offer = new TransportOffer(
            request.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            350m,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));

        db.TransportRequests.Add(request);
        db.TransportOffers.Add(offer);
        await db.SaveChangesAsync();

        var handler = new RejectTransportOfferHandler(
            db,
            new MockCurrentUserService(customerId));

        await handler.Handle(offer.Id);

        Assert.Equal(
            TransportOfferStatus.Rejected,
            offer.Status);
    }
}