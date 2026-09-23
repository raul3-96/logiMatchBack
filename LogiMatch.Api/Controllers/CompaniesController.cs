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
    private readonly GetCompanyMembersHandler _getMembersHandler;
    private readonly ActivateCompanyMemberHandler _activateMemberHandler;
    private readonly DeactivateCompanyMemberHandler _deactivateMemberHandler;
    private readonly PromoteCompanyMemberHandler _promoteMemberHandler;
    private readonly DemoteCompanyMemberHandler _demoteMemberHandler;

    public CompaniesController(
        CreateCompanyHandler createHandler,
        GetCompanyHandler getHandler,
        AddCompanyMemberHandler addMemberHandler,
        GetCompanyMembersHandler getMembersHandler,
        ActivateCompanyMemberHandler activateMemberHandler,
        DeactivateCompanyMemberHandler deactivateMemberHandler,
        PromoteCompanyMemberHandler promoteMemberHandler,
        DemoteCompanyMemberHandler demoteMemberHandler)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
        _addMemberHandler = addMemberHandler;
        _getMembersHandler = getMembersHandler;
        _activateMemberHandler = activateMemberHandler;
        _deactivateMemberHandler = deactivateMemberHandler;
        _promoteMemberHandler = promoteMemberHandler;
        _demoteMemberHandler = demoteMemberHandler;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCompanyCommand command)
    {
        var id = await _createHandler.Handle(command);

        return Created(
            $"/api/companies/{id}",
            new { id });
    }

    [Authorize]
    [HttpPost("{companyId:guid}/members")]
    public async Task<IActionResult> AddMember(
        Guid companyId,
        AddCompanyMemberCommand command)
    {
        var id = await _addMemberHandler.Handle(companyId, command);

        return Created(
            $"/api/companies/{companyId}/members/{id}",
            new { id });
    }

    [Authorize]
    [HttpGet("{companyId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid companyId)
    {
        var members = await _getMembersHandler.Handle(companyId);

        return Ok(members);
    }

    [Authorize]
    [HttpPost("{companyId:guid}/members/{memberId:guid}/activate")]
    public async Task<IActionResult> ActivateMember(
        Guid companyId,
        Guid memberId)
    {
        await _activateMemberHandler.Handle(companyId, memberId);

        return NoContent();
    }

    [Authorize]
    [HttpPost("{companyId:guid}/members/{memberId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateMember(
        Guid companyId,
        Guid memberId)
    {
        await _deactivateMemberHandler.Handle(companyId, memberId);

        return NoContent();
    }

    [Authorize]
    [HttpPost("{companyId:guid}/members/{memberId:guid}/promote")]
    public async Task<IActionResult> PromoteMember(
        Guid companyId,
        Guid memberId)
    {
        await _promoteMemberHandler.Handle(companyId, memberId);

        return NoContent();
    }

    [Authorize]
    [HttpPost("{companyId:guid}/members/{memberId:guid}/demote")]
    public async Task<IActionResult> DemoteMember(
        Guid companyId,
        Guid memberId)
    {
        await _demoteMemberHandler.Handle(companyId, memberId);

        return NoContent();
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