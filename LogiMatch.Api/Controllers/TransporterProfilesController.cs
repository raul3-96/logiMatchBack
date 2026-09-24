using LogiMatch.Application.TransporterProfiles;
using LogiMatch.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LogiMatch.Application;
using Microsoft.EntityFrameworkCore;

namespace LogiMatch.Api.Controllers;

[ApiController]
[Route("api/transporter-profiles")]
[Authorize]
public class TransporterProfilesController : ControllerBase
{
    private readonly CreateTransporterProfileHandler _createHandler;
    private readonly GetTransporterProfileHandler _getHandler;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public TransporterProfilesController(
        CreateTransporterProfileHandler createHandler,
        GetTransporterProfileHandler getHandler,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
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
    [AllowAnonymous]
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
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id)
    {
        var publicProfile = await _getHandler.Handle(id);
        return Ok(publicProfile);
    }
}