using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LogiMatch.Application.Companies;

namespace LogiMatch.Api.Controllers;

[ApiController]
[Route("api/companies")]
public class CompaniesController : ControllerBase
{
    private readonly CreateCompanyHandler _createHandler;
    private readonly GetCompanyHandler _getHandler;

    public CompaniesController(
        CreateCompanyHandler createHandler,
        GetCompanyHandler getHandler)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateCompanyCommand command)
    {
        var id = await _createHandler.Handle(command);

        return Created(
            $"/api/companies/{id}",
            new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var companies = await _getHandler.HandleAll();

        return Ok(companies);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var company = await _getHandler.Handle(id);

        if (company == null)
            return NotFound();

        return Ok(company);
    }
}
