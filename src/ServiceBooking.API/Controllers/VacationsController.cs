using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Vacations;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Authorize(Roles = "Specialist")]
[Route("api/v1/vacations")]
public sealed class VacationsController(IVacationService vacationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<VacationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<VacationResponse>>> List(CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await vacationService.ListForSpecialistAsync(specialistId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPost]
    [ProducesResponseType<VacationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VacationResponse>> Create(
        UpsertVacationRequest request,
        CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await vacationService.CreateAsync(specialistId.Value, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToErrorResult(result);
        }

        return Created($"api/v1/vacations/{result.Value!.Id}", result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<VacationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VacationResponse>> Update(
        Guid id,
        UpsertVacationRequest request,
        CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await vacationService.UpdateAsync(specialistId.Value, id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await vacationService.DeleteAsync(specialistId.Value, id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToBooleanErrorResult(result);
    }
}
