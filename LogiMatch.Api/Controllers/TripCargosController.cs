using LogiMatch.Application.Matching;
using LogiMatch.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LogiMatch.Api.Controllers;

[ApiController]
[Route("api/trip-cargos")]
public class TripCargosController : ControllerBase
{
    private readonly GetTripCargoHandler _getHandler;
    private readonly StartTripCargoHandler _startHandler;
    private readonly CompleteTripCargoHandler _completeHandler;
    private readonly CancelTripCargoHandler _cancelHandler;

    public TripCargosController(
        GetTripCargoHandler getHandler,
        StartTripCargoHandler startHandler,
        CompleteTripCargoHandler completeHandler,
        CancelTripCargoHandler cancelHandler)
    {
        _getHandler = getHandler;
        _startHandler = startHandler;
        _completeHandler = completeHandler;
        _cancelHandler = cancelHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? tripId,
        [FromQuery] Guid? transportRequestId,
        [FromQuery] TripCargoStatus? status)
    {
        var tripCargos = await _getHandler.HandleAll(
            tripId,
            transportRequestId,
            status);

        return Ok(tripCargos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var tripCargo = await _getHandler.Handle(id);

        if (tripCargo == null)
            return NotFound();

        return Ok(tripCargo);
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
}