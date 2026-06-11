using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Catalog;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Route("api/v1/services")]
public sealed class ServicesController(IServiceCatalogService serviceCatalog) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<ServiceResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ServiceResponse>>> List(CancellationToken cancellationToken)
    {
        var result = await serviceCatalog.ListAsync(cancellationToken);
        return Ok(result.Value);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await serviceCatalog.GetAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [Authorize(Roles = "Specialist")]
    [HttpPost]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceResponse>> Create(
        UpsertServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await serviceCatalog.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToErrorResult(result);
        }

        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [Authorize(Roles = "Specialist")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceResponse>> Update(
        Guid id,
        UpsertServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await serviceCatalog.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [Authorize(Roles = "Specialist")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await serviceCatalog.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToBooleanErrorResult(result);
    }
}
