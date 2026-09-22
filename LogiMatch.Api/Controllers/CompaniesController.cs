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
    private readonly AddCompanyMemberHandler _addMemberHandler;

    public CompaniesController(
        CreateCompanyHandler createHandler,
        GetCompanyHandler getHandler,
        AddCompanyMemberHandler addMemberHandler)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
        _addMemberHandler = addMemberHandler;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCompanyCommand command)
    {
        var id = await _createHandler.Handle(command);
        return Created($"/api/companies/{id}", new { id });
    }

    [Authorize]
    [HttpPost("{companyId:guid}/members")]
    public async Task<IActionResult> AddMember(
        Guid companyId,
        AddCompanyMemberCommand command)
    {
        var id = await _addMemberHandler.Handle(companyId, command);
        return Created($"/api/companies/{companyId}/members/{id}", new { id });
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
