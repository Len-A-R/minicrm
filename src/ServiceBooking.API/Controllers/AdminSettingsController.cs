using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Admin;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin")]
public sealed class AdminSettingsController(IAdminActionService adminActions) : ControllerBase
{
    [HttpGet("settings")]
    public async Task<ActionResult<IReadOnlyCollection<AdminSettingResponse>>> ListSettings(CancellationToken cancellationToken)
    {
        var result = await adminActions.ListSettingsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPut("settings")]
    public async Task<ActionResult<AdminSettingResponse>> UpsertSetting(
        UpsertSystemSettingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.UpsertSettingAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpDelete("settings/{id:guid}")]
    public async Task<IActionResult> DeleteSetting(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminActions.DeleteSettingAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToBooleanErrorResult(result);
    }

    [HttpGet("admins")]
    public async Task<ActionResult<IReadOnlyCollection<AdminUserResponse>>> ListAdmins(CancellationToken cancellationToken)
    {
        var result = await adminActions.ListAdminsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPost("admins")]
    public async Task<ActionResult<AdminUserResponse>> CreateAdmin(
        UpsertAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.UpsertAdminAsync(null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPut("admins/{id:guid}")]
    public async Task<ActionResult<AdminUserResponse>> UpdateAdmin(
        Guid id,
        UpsertAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminActions.UpsertAdminAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpDelete("admins/{id:guid}")]
    public async Task<IActionResult> DeleteAdmin(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminActions.DeleteAdminAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToBooleanErrorResult(result);
    }
}
