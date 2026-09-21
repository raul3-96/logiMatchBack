using LogiMatch.Application.Bookings;
using LogiMatch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogiMatch.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly GetBookingHandler _getHandler;
    private readonly StartBookingHandler _startHandler;
    private readonly CompleteBookingHandler _completeHandler;
    private readonly CancelBookingHandler _cancelHandler;

    public BookingsController(
        GetBookingHandler getHandler,
        StartBookingHandler startHandler,
        CompleteBookingHandler completeHandler,
        CancelBookingHandler cancelHandler)
    {
        _getHandler = getHandler;
        _startHandler = startHandler;
        _completeHandler = completeHandler;
        _cancelHandler = cancelHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? transportRequestId,
        [FromQuery] Guid? transporterProfileId,
        [FromQuery] BookingStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var bookings = await _getHandler.HandleAll(
            transportRequestId,
            transporterProfileId,
            status,
            page,
            pageSize);

        return Ok(bookings);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var booking = await _getHandler.Handle(id);

        if (booking == null)
            return NotFound();

        return Ok(booking);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id)
    {
        await _startHandler.Handle(id);

        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        await _completeHandler.Handle(id);

        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _cancelHandler.Handle(id);

        return NoContent();
    }
}