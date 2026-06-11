using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Admin;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/subscriptions")]
public sealed class AdminSubscriptionsController(IAdminActionService adminActions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AdminSubscriptionResponse>>> List(
        [FromQuery] SubscriptionStatus? status,
        [FromQuery] Guid? specialistId,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.ListSubscriptionsAsync(status, specialistId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<AdminSubscriptionResponse>> ChangeStatus(
        Guid id,
        AdminSubscriptionStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.ChangeSubscriptionStatusAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPut("{id:guid}/renew")]
    public async Task<ActionResult<AdminSubscriptionResponse>> Renew(
        Guid id,
        RenewSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.RenewSubscriptionAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }
}
