using LogiMatch.Application.Authentication;
using LogiMatch.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogiMatch.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly LoginUserHandler _loginHandler;

    public AuthController(LoginUserHandler loginHandler)
    {
        _loginHandler = loginHandler;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginUserCommand command)
    {
        var result = await _loginHandler.Handle(command);

        return Ok(result);
    }
}