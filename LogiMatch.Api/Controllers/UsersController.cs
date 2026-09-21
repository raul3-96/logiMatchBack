using LogiMatch.Application.Users;
using LogiMatch.Domain;
using LogiMatch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogiMatch.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly CreateUserHandler _createHandler;
    private readonly GetUserHandler _getHandler;

    public UsersController(
        CreateUserHandler createHandler,
        GetUserHandler getHandler)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateUserCommand command)
    {
        var id = await _createHandler.Handle(command);

        return Created(
            $"/api/users/{id}",
            new { id });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] UserStatus? status)
    {
        var users = await _getHandler.HandleAll(status);

        return Ok(users);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var user = await _getHandler.Handle(id);

        if (user == null)
            return NotFound();

        return Ok(user);
    }
}