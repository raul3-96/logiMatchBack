using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Tests.Common;
using LogiMatch.Application.TransportRequests;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMatch.Application.Tests.TransportRequests;

public class PublishTransportRequestHandlerTests
{
    [Fact]
    public async Task Handle_WhenRequestDoesNotExist_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new PublishTransportRequestHandler(db,new MockCurrentUserService(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(Guid.NewGuid()));

        Assert.Equal(
            "The specified transport request does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestHasNoCargo_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var user= new MockCurrentUserService(Guid.NewGuid());
        var request = CreateRequest(user.UserId);
        db.TransportRequests.Add(request);
        await db.SaveChangesAsync();

        var handler = new PublishTransportRequestHandler(db, user);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(request.Id));

        Assert.Equal(
            "A transport request must have at least one cargo before it can be published.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestIsAlreadyPublished_ShouldThrow()
    {
        using var db = TestDbContextFactory.Create();

        var user= new MockCurrentUserService(Guid.NewGuid());
        var request = CreateRequest(user.UserId);
        request.Publish();

        db.TransportRequests.Add(request);
        db.Cargos.Add(CreateCargo(request.Id));
        await db.SaveChangesAsync();

        var handler = new PublishTransportRequestHandler(db,user);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(request.Id));

        Assert.Equal(
            "Only draft requests can be published.",
            exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRequestHasCargo_ShouldPublishRequest()
    {
        using var db = TestDbContextFactory.Create();

        var user= new MockCurrentUserService(Guid.NewGuid());
        var request = CreateRequest(user.UserId);

        db.TransportRequests.Add(request);
        db.Cargos.Add(CreateCargo(request.Id));
        await db.SaveChangesAsync();

        var handler = new PublishTransportRequestHandler(db,user);

        await handler.Handle(request.Id);

        var savedRequest = await db.TransportRequests
            .SingleAsync(x => x.Id == request.Id);

        Assert.Equal(
            TransportRequestStatus.Published,
            savedRequest.Status);
    }

    private static TransportRequest CreateRequest(Guid userId)
    {
        return new TransportRequest(
            userId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(8));
    }

    private static Cargo CreateCargo(Guid requestId)
    {
        return new Cargo(
            requestId,
            "Test cargo",
            850m,
            4.5m,
            10,
            false,
            false);
    }
}