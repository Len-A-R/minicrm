using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Catalog;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Route("api/v1/locations")]
public sealed class LocationsController(ILocationService locationService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<LocationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<LocationResponse>>> List(
        [FromQuery] Guid? serviceId,
        CancellationToken cancellationToken)
    {
        var result = await locationService.ListAsync(serviceId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType<LocationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await locationService.GetAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [Authorize(Roles = "Specialist")]
    [HttpPost]
    [ProducesResponseType<LocationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LocationResponse>> Create(
        UpsertLocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await locationService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToErrorResult(result);
        }

        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [Authorize(Roles = "Specialist")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType<LocationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationResponse>> Update(
        Guid id,
        UpsertLocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await locationService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [Authorize(Roles = "Specialist")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await locationService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToBooleanErrorResult(result);
    }
}
