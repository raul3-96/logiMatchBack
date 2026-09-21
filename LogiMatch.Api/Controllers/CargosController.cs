using LogiMatch.Application.Cargos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogiMatch.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cargos")]
public class CargosController : ControllerBase
{
    private readonly CreateCargoHandler _createHandler;
    private readonly GetCargoHandler _getHandler;

    public CargosController(
        CreateCargoHandler createHandler,
        GetCargoHandler getHandler)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateCargoCommand command)
    {
        var id = await _createHandler.Handle(command);

        return Created(
            $"/api/cargos/{id}",
            new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? transportRequestId)
    {
        var cargos = await _getHandler.HandleAll(
            transportRequestId);

        return Ok(cargos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var cargo = await _getHandler.Handle(id);

        if (cargo == null)
            return NotFound();

        return Ok(cargo);
    }
}