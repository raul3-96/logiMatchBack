using LogiMatch.Application.Locations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogiMatch.Api.Controllers;

[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly CreateLocationHandler _createHandler;
    private readonly GetLocationHandler _getHandler;

    public LocationsController(
        CreateLocationHandler createHandler,
        GetLocationHandler getHandler)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateLocationCommand command)
    {
        var id = await _createHandler.Handle(command);

        return Created(
            $"/api/locations/{id}",
            new { id });
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var location = await _getHandler.Handle(id);

        if (location == null)
            return NotFound();

        return Ok(location);
    }
}