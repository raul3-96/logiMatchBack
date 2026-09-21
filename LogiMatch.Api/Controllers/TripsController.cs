using LogiMatch.Application.Matching;
using LogiMatch.Application.Trips;
using LogiMatch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogiMatch.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/trips")]
public class TripsController : ControllerBase
{
    private readonly CreateTripHandler _createHandler;
    private readonly StartTripHandler _startHandler;
    private readonly CompleteTripHandler _completeHandler;
    private readonly CancelTripHandler _cancelHandler;
    private readonly GetTripHandler _getHandler;
    private readonly ReserveTripCapacityHandler _reserveHandler;

    public TripsController(
        CreateTripHandler createHandler,
        GetTripHandler getHandler,
        StartTripHandler startHandler,
        CompleteTripHandler completeHandler,
        CancelTripHandler cancelHandler,
        ReserveTripCapacityHandler reserveHandler)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
        _startHandler = startHandler;
        _completeHandler = completeHandler;
        _cancelHandler = cancelHandler;
        _reserveHandler = reserveHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateTripCommand command)
    {
        var id = await _createHandler.Handle(command);

        return Created(
            $"/api/trips/{id}",
            new { id });
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id)
    {
        await _startHandler.Handle(id);

        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        await _completeHandler.Handle(id);

        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _cancelHandler.Handle(id);

        return NoContent();
    }

    [HttpPost("{id:guid}/reserve")]
    public async Task<IActionResult> Reserve(
        Guid id,
        ReserveTripCapacityCommand command)
    {
        command.TripId = id;

        var tripCargoId = await _reserveHandler.Handle(command);

        return Created(
            $"/api/trip-cargos/{tripCargoId}",
            new { tripCargoId });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? transporterProfileId,
        [FromQuery] Guid? vehicleId,
        [FromQuery] TripStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var trips = await _getHandler.Handle(
            transporterProfileId,
            vehicleId,
            status,
            page,
            pageSize);

        return Ok(trips);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await _getHandler.Handle(
            new GetTripCommand
            {
                TripId = id
            });

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}