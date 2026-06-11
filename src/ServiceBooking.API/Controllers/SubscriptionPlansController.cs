using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Admin;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Route("api/v1/subscription-plans")]
public sealed class SubscriptionPlansController(IAdminActionService adminActions) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<SubscriptionPlanResponse>>> List(CancellationToken cancellationToken)
    {
        var result = await adminActions.ListPlansAsync(activeOnly: true, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SubscriptionPlanResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminActions.GetPlanAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<SubscriptionPlanResponse>> Create(
        UpsertSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.CreatePlanAsync(request, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : this.ToErrorResult(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SubscriptionPlanResponse>> Update(
        Guid id,
        UpsertSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.UpdatePlanAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminActions.DeletePlanAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToBooleanErrorResult(result);
    }
}
