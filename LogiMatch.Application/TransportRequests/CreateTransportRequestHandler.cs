using LogiMatch.Application;
using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.TransportRequests;

public class CreateTransportRequestHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateTransportRequestHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        CreateTransportRequestCommand command)
    {
        // Validar que el cliente que crea la solicitud es el usuario actual
        if (command.CustomerId != _currentUserService.UserId)
            throw new ConflictException(
                "You cannot create a transport request on behalf of another customer.");

        var customer = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == command.CustomerId);

        if (customer == null)
            throw new NotFoundException(
                "The specified customer does not exist.");

        if (customer.Status != UserStatus.Active)
            throw new ConflictException(
                "The specified customer is not active.");

        var pickupExists = await _dbContext.Locations
            .AnyAsync(x => x.Id == command.PickupLocationId);

        if (!pickupExists)
            throw new NotFoundException(
                "The specified pickup location does not exist.");

        var deliveryExists = await _dbContext.Locations
            .AnyAsync(x => x.Id == command.DeliveryLocationId);

        if (!deliveryExists)
            throw new NotFoundException(
                "The specified delivery location does not exist.");

        if (command.PickupLocationId == command.DeliveryLocationId)
            throw new ValidationException(
                "Pickup and delivery locations must be different.");

        if (command.PickupDate < DateTime.UtcNow)
            throw new ValidationException(
                "Pickup date cannot be in the past.");

        if (command.DeliveryDate.HasValue &&
            command.DeliveryDate.Value < command.PickupDate)
        {
            throw new ValidationException(
                "Delivery date cannot be before pickup date.");
        }

        var request = new TransportRequest(
            command.CustomerId,
            command.PickupLocationId,
            command.DeliveryLocationId,
            command.PickupDate,
            command.DeliveryDate);

        _dbContext.TransportRequests.Add(request);

        await _dbContext.SaveChangesAsync();

        return request.Id;
    }
}