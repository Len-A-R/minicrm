using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Slots;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Route("api/v1/specialists/{specialistId:guid}/slots")]
public sealed class SlotsController(ISlotService slotService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<AvailableSlotResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<AvailableSlotResponse>>> GetSlots(
        Guid specialistId,
        [FromQuery] DateOnly date,
        [FromQuery] int durationMinutes = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await slotService.GetAvailableSlotsAsync(
            specialistId,
            date,
            durationMinutes,
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }
}
