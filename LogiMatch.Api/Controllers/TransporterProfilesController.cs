using LogiMatch.Application.TransporterProfiles;
using Microsoft.AspNetCore.Mvc;

namespace LogiMatch.Api.Controllers;

[ApiController]
[Route("api/transporter-profiles")]
public class TransporterProfilesController : ControllerBase
{
    private readonly CreateTransporterProfileHandler _createHandler;
    private readonly GetTransporterProfileHandler _getHandler;

    public TransporterProfilesController(
        CreateTransporterProfileHandler createHandler,
        GetTransporterProfileHandler getHandler)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateTransporterProfileCommand command)
    {
        var id = await _createHandler.Handle(command);

        return Created(
            $"/api/transporter-profiles/{id}",
            new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? userId,
        [FromQuery] Guid? companyId)
    {
        var profiles = await _getHandler.HandleAll(
            userId,
            companyId);

        return Ok(profiles);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var profile = await _getHandler.Handle(id);

        if (profile == null)
            return NotFound();

        return Ok(profile);
    }
}