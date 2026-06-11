using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Admin;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/bookings")]
public sealed class AdminBookingsController(IAdminActionService adminActions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AdminBookingResponse>>> List(
        [FromQuery] BookingStatus? status,
        [FromQuery] Guid? specialistId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.ListBookingsAsync(status, specialistId, from, to, search, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminBookingResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminActions.GetBookingAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<AdminBookingResponse>> ChangeStatus(
        Guid id,
        AdminBookingStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.ChangeBookingStatusAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminActions.DeleteBookingAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToBooleanErrorResult(result);
    }
}
