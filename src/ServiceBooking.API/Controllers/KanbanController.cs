using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Kanban;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/kanban")]
public sealed class KanbanController(IKanbanService kanbanService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<KanbanBoardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<KanbanBoardResponse>> GetBoard(
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await kanbanService.GetBoardAsync(
            specialistId.Value,
            new KanbanBoardQuery(date),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPut("{bookingId:guid}/move")]
    [ProducesResponseType<KanbanBookingCardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<KanbanBookingCardResponse>> Move(
        Guid bookingId,
        MoveKanbanBookingRequest request,
        CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await kanbanService.MoveAsync(
            specialistId.Value,
            bookingId,
            request,
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }
}
