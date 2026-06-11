using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Admin;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/clients")]
public sealed class AdminClientsController(IAdminActionService adminActions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AdminClientResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] ClientStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.ListClientsAsync(search, status, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminClientResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminActions.GetClientAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminClientResponse>> Update(
        Guid id,
        AdminClientUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.UpdateClientAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminActions.DeleteClientAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToBooleanErrorResult(result);
    }
}
