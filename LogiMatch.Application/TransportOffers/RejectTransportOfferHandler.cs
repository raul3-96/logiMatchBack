using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.TransportOffers;

public class RejectTransportOfferHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public RejectTransportOfferHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task Handle(Guid offerId)
    {
        var offer = await _dbContext.TransportOffers
            .FirstOrDefaultAsync(x => x.Id == offerId);

        if (offer == null)
            throw new NotFoundException(
                "The specified transport offer does not exist.");

        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(x => x.Id == offer.TransportRequestId);

        if (request == null)
            throw new NotFoundException(
                "The transport request associated with the offer does not exist.");

        if (request.CustomerId != _currentUserService.UserId)
            throw new ConflictException(
                "Only the owner of the transport request can reject offers.");

        if (offer.Status != TransportOfferStatus.Pending)
            throw new ConflictException(
                "Only pending offers can be rejected.");

        offer.Reject();

        await _dbContext.SaveChangesAsync();
    }
}