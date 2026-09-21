using LogiMatch.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogiMatch.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly CreateVehicleHandler _createHandler;
    private readonly GetVehicleHandler _getHandler;

    public VehiclesController(
        CreateVehicleHandler createHandler,
        GetVehicleHandler getHandler)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateVehicleCommand command)
    {
        var id = await _createHandler.Handle(command);

        return Created(
            $"/api/vehicles/{id}",
            new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? transporterProfileId)
    {
        var vehicles = await _getHandler.HandleAll(
            transporterProfileId);

        return Ok(vehicles);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var vehicle = await _getHandler.Handle(id);

        if (vehicle == null)
            return NotFound();

        return Ok(vehicle);
    }
}