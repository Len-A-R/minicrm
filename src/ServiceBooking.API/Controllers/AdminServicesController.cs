using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Admin;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/services")]
public sealed class AdminServicesController(IAdminActionService adminActions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AdminServiceResponse>>> List(CancellationToken cancellationToken)
    {
        var result = await adminActions.ListServicesAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminServiceResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminActions.GetServiceAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<AdminServiceResponse>> Create(UpsertAdminServiceRequest request, CancellationToken cancellationToken)
    {
        var result = await adminActions.CreateServiceAsync(request, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : this.ToErrorResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminServiceResponse>> Update(
        Guid id,
        UpsertAdminServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.UpdateServiceAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminActions.DeleteServiceAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToBooleanErrorResult(result);
    }
}
