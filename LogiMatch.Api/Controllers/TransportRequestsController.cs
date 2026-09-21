using LogiMatch.Application.TransportRequests;
using LogiMatch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogiMatch.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/transport-requests")]
public class TransportRequestsController : ControllerBase
{
    private readonly CreateTransportRequestHandler _handler;
    private readonly GetTransportRequestHandler _getHandler;
    private readonly PublishTransportRequestHandler _publishHandler;
    private readonly CancelTransportRequestHandler _cancelHandler;

    public TransportRequestsController(
        CreateTransportRequestHandler handler,
        GetTransportRequestHandler getHandler,
        PublishTransportRequestHandler publishHandler,
        CancelTransportRequestHandler cancelHandler)
    {
        _handler = handler;
        _getHandler = getHandler;
        _publishHandler = publishHandler;
        _cancelHandler = cancelHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateTransportRequestCommand command)
    {
        var id = await _handler.Handle(command);

        return Created(
            $"/api/transport-requests/{id}",
            new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? customerId,
        [FromQuery] TransportRequestStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var requests = await _getHandler.Handle(
            customerId,
            status,
            page,
            pageSize);

        return Ok(requests);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var request = await _getHandler.Handle(id);

        if (request == null)
            return NotFound();

        return Ok(request);
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id)
    {
        await _publishHandler.Handle(id);

        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _cancelHandler.Handle(id);

        return NoContent();
    }
}