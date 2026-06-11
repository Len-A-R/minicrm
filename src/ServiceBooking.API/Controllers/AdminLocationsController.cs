using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Admin;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/locations")]
public sealed class AdminLocationsController(IAdminActionService adminActions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AdminLocationResponse>>> List(CancellationToken cancellationToken)
    {
        var result = await adminActions.ListLocationsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminLocationResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminActions.GetLocationAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<AdminLocationResponse>> Create(UpsertAdminLocationRequest request, CancellationToken cancellationToken)
    {
        var result = await adminActions.CreateLocationAsync(request, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : this.ToErrorResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminLocationResponse>> Update(
        Guid id,
        UpsertAdminLocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.UpdateLocationAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminActions.DeleteLocationAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToBooleanErrorResult(result);
    }
}
