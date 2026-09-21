using LogiMatch.Application.Matching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogiMatch.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/matching")]
public class MatchingController : ControllerBase
{
    private readonly FindMatchingVehiclesHandler _findMatchingVehiclesHandler;
    private readonly FindMatchingTripsHandler _findMatchingTripsHandler;
    private readonly ReserveTripCapacityHandler _reserveTripCapacityHandler;

    public MatchingController(
        FindMatchingVehiclesHandler findMatchingVehiclesHandler,
        FindMatchingTripsHandler findMatchingTripsHandler,
        ReserveTripCapacityHandler reserveTripCapacityHandler)
    {
        _findMatchingVehiclesHandler = findMatchingVehiclesHandler;
        _findMatchingTripsHandler = findMatchingTripsHandler;
        _reserveTripCapacityHandler = reserveTripCapacityHandler;
    }

    [HttpPost("vehicles")]
    public async Task<IActionResult> FindMatchingVehicles(
        FindMatchingVehiclesCommand command)
    {
        var result = await _findMatchingVehiclesHandler.Handle(command);

        return Ok(result);
    }

    [HttpPost("trips")]
    public async Task<IActionResult> FindMatchingTrips(
        FindMatchingTripsCommand command)
    {
        var result = await _findMatchingTripsHandler.Handle(command);

        return Ok(result);
    }

    [HttpPost("trips/reserve")]
    public async Task<IActionResult> ReserveTrip(
        ReserveTripCapacityCommand command)
    {
        var tripCargoId =
            await _reserveTripCapacityHandler.Handle(command);

        return Ok(new { tripCargoId });
    }
}