using LogiMatch.Application.Matching;
using Microsoft.AspNetCore.Mvc;

namespace LogiMatch.Api.Controllers;

[ApiController]
[Route("api/matching")]
public class MatchingController : ControllerBase
{
    private readonly FindMatchingVehiclesHandler _findVehiclesHandler;
    private readonly FindMatchingTripsHandler _findTripsHandler;
    private readonly ReserveTripCapacityHandler _reserveTripCapacityHandler;

    public MatchingController(
        FindMatchingVehiclesHandler findVehiclesHandler,
        FindMatchingTripsHandler findTripsHandler,
        ReserveTripCapacityHandler reserveTripCapacityHandler)
    {
        _findVehiclesHandler = findVehiclesHandler;
        _findTripsHandler = findTripsHandler;
        _reserveTripCapacityHandler = reserveTripCapacityHandler;
    }

    [HttpPost("vehicles")]
    public async Task<IActionResult> FindVehicles(
        FindMatchingVehiclesCommand command)
    {
        var result = await _findVehiclesHandler.Handle(command);

        return Ok(result);
    }

    [HttpPost("trips")]
    public async Task<IActionResult> FindTrips(
        FindMatchingTripsCommand command)
    {
        var result = await _findTripsHandler.Handle(command);

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