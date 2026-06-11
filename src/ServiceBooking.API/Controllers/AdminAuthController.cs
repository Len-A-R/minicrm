using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Admin;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Route("api/v1/admin/auth")]
public sealed class AdminAuthController(IAdminAuthService adminAuthService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AdminAuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdminAuthResponse>> Login(AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await adminAuthService.LoginAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("me")]
    [ProducesResponseType<AdminMeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdminMeResponse>> Me(CancellationToken cancellationToken)
    {
        var adminId = User.GetCurrentSpecialistId();
        if (adminId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain admin id."));
        }

        var result = await adminAuthService.GetMeAsync(adminId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }
}
