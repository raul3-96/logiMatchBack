using LogiMatch.Application.Common.Exceptions;
using LogiMatch.Application.Common.Interfaces;
using LogiMatch.Domain;
using LogiMatch.Domain.Entities;
using LogiMatch.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Application.Cargos;

public class CreateCargoHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateCargoHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        CreateCargoCommand command)
    {
        var request = await _dbContext.TransportRequests
            .FirstOrDefaultAsync(x => x.Id == command.TransportRequestId);

        if (request == null)
            throw new NotFoundException(
                "The specified transport request does not exist.");

        // Validar que el usuario actual es el dueño de la solicitud
        if (request.CustomerId != _currentUserService.UserId)
            throw new ConflictException(
                "You can only add cargo to your own transport requests.");

        if (request.Status != TransportRequestStatus.Draft &&
            request.Status != TransportRequestStatus.Published)
        {
            throw new ConflictException(
                "Cargo can only be added to draft or published transport requests.");
        }

        var tripCargoExists = await _dbContext.TripCargos
            .AnyAsync(x =>
                x.TransportRequestId == command.TransportRequestId &&
                x.Status != TripCargoStatus.Cancelled);

        if (tripCargoExists)
            throw new ConflictException(
                "Cargo cannot be added because the transport request is already assigned to a trip.");

        var cargo = new Cargo(
            command.TransportRequestId,
            command.Description,
            command.WeightKg,
            command.VolumeM3,
            command.Quantity,
            command.RequiresRefrigeration,
            command.RequiresTailLift);

        _dbContext.Cargos.Add(cargo);

        await _dbContext.SaveChangesAsync();

        return cargo.Id;
    }
}