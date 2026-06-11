using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Admin;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/specialists")]
public sealed class AdminSpecialistsController(IAdminActionService adminActions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AdminSpecialistResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] bool? blocked,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.ListSpecialistsAsync(search, blocked, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminSpecialistResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminActions.GetSpecialistAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPut("{id:guid}/block")]
    public async Task<ActionResult<AdminSpecialistResponse>> Block(Guid id, BlockSpecialistRequest request, CancellationToken cancellationToken)
    {
        var result = await adminActions.BlockSpecialistAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPut("{id:guid}/unblock")]
    public async Task<ActionResult<AdminSpecialistResponse>> Unblock(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminActions.UnblockSpecialistAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPut("{id:guid}/plan")]
    public async Task<ActionResult<AdminSpecialistResponse>> ChangePlan(
        Guid id,
        ChangeSpecialistPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.ChangeSpecialistPlanAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminActions.DeleteSpecialistAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToBooleanErrorResult(result);
    }
}
