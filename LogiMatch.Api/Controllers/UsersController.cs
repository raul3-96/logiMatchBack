using LogiMatch.Application.Users;
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

    [AllowAnonymous]
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
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var user = await _getHandler.HandleMe();

        if (user == null)
            return NotFound();

        return Ok(user);
    }
}