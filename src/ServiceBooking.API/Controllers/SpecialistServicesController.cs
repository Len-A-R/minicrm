using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.SpecialistServices;

namespace ServiceBooking.API.Controllers;

[ApiController]
public sealed class SpecialistServicesController(ISpecialistServicesService specialistServices) : ControllerBase
{
    [Authorize(Roles = "Specialist")]
    [HttpGet("api/v1/specialist-services")]
    [ProducesResponseType<IReadOnlyCollection<SpecialistServiceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<SpecialistServiceResponse>>> ListMine(
        CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await specialistServices.ListForSpecialistAsync(specialistId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [AllowAnonymous]
    [HttpGet("api/v1/specialists/{specialistId:guid}/services")]
    [ProducesResponseType<IReadOnlyCollection<SpecialistServiceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<SpecialistServiceResponse>>> ListForSpecialist(
        Guid specialistId,
        CancellationToken cancellationToken)
    {
        var result = await specialistServices.ListForSpecialistAsync(specialistId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [Authorize(Roles = "Specialist")]
    [HttpPost("api/v1/specialist-services")]
    [ProducesResponseType<SpecialistServiceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SpecialistServiceResponse>> Create(
        UpsertSpecialistServiceRequest request,
        CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await specialistServices.CreateAsync(specialistId.Value, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToErrorResult(result);
        }

        return Created($"api/v1/specialist-services/{result.Value!.Id}", result.Value);
    }

    [Authorize(Roles = "Specialist")]
    [HttpPut("api/v1/specialist-services/{id:guid}")]
    [ProducesResponseType<SpecialistServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SpecialistServiceResponse>> Update(
        Guid id,
        UpsertSpecialistServiceRequest request,
        CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await specialistServices.UpdateAsync(specialistId.Value, id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [Authorize(Roles = "Specialist")]
    [HttpDelete("api/v1/specialist-services/{id:guid}")]
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

        var result = await specialistServices.DeleteAsync(specialistId.Value, id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToBooleanErrorResult(result);
    }
}
