using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Admin;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Route("api/v1/admin/payments")]
public sealed class AdminPaymentsController(
    IAdminActionService adminActions,
    IPaymentService paymentService) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AdminPaymentResponse>>> List(
        [FromQuery] PaymentStatus? status,
        [FromQuery] Guid? specialistId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.ListPaymentsAsync(status, specialistId, from, to, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("summary")]
    public async Task<ActionResult<PlatformFinanceSummaryResponse>> Summary(CancellationToken cancellationToken)
    {
        var result = await adminActions.GetFinanceSummaryAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminPaymentResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminActions.GetPaymentAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<AdminPaymentResponse>> Create(PaymentCreateRequest request, CancellationToken cancellationToken)
    {
        var result = await paymentService.CreateAsync(request, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : this.ToErrorResult(result);
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<ActionResult<AdminPaymentResponse>> Webhook(PaymentWebhookRequest request, CancellationToken cancellationToken)
    {
        var result = await paymentService.ProcessWebhookAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }
}
