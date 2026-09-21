using LogiMatch.Application.VehicleAvailabilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogiMatch.Api.Controllers;

[ApiController]
[Route("api/vehicle-availabilities")]
[Authorize]
public class VehicleAvailabilitiesController : ControllerBase
{
    private readonly CreateVehicleAvailabilityHandler _createHandler;
    private readonly GetVehicleAvailabilityHandler _getHandler;

    public VehicleAvailabilitiesController(
        CreateVehicleAvailabilityHandler createHandler,
        GetVehicleAvailabilityHandler getHandler)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateVehicleAvailabilityCommand command)
    {
        var id = await _createHandler.Handle(command);

        return Created(
            $"/api/vehicle-availabilities/{id}",
            new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? vehicleId)
    {
        var availabilities = await _getHandler.HandleAll(vehicleId);

        return Ok(availabilities);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var availability = await _getHandler.Handle(id);

        if (availability == null)
            return NotFound();

        return Ok(availability);
    }
}