using LogiMatch.Application.TransportOffers;
using LogiMatch.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LogiMatch.Api.Controllers;

[ApiController]
[Route("api/transport-offers")]
public class TransportOffersController : ControllerBase
{
    private readonly CreateTransportOfferHandler _createHandler;
    private readonly GetTransportOffersHandler _getHandler;
    private readonly AcceptTransportOfferHandler _acceptHandler;

    public TransportOffersController(
        CreateTransportOfferHandler createHandler,
        GetTransportOffersHandler getHandler,
        AcceptTransportOfferHandler acceptHandler)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
        _acceptHandler = acceptHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateTransportOfferCommand command)
    {
        var id = await _createHandler.Handle(command);

        return Created(
            $"/api/transport-offers/{id}",
            new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? transportRequestId,
        [FromQuery] Guid? transporterProfileId,
        [FromQuery] Guid? vehicleId,
        [FromQuery] TransportOfferStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var offers = await _getHandler.HandleAll(
            transportRequestId,
            transporterProfileId,
            vehicleId,
            status,
            page,
            pageSize);

        return Ok(offers);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var offer = await _getHandler.Handle(id);

        if (offer == null)
            return NotFound();

        return Ok(offer);
    }

    [HttpGet("request/{transportRequestId:guid}")]
    public async Task<IActionResult> GetByRequest(
        Guid transportRequestId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var offers = await _getHandler.HandleByRequest(
            transportRequestId,
            page,
            pageSize);

        return Ok(offers);
    }

    [HttpPost("{offerId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid offerId)
    {
        var bookingId = await _acceptHandler.Handle(offerId);

        return Ok(new { bookingId });
    }
}