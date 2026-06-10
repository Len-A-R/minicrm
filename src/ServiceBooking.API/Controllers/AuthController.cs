using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Common;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterSpecialistRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result);
        }

        return CreatedAtAction(nameof(GetMe), result.Value);
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToErrorResult(result);
    }

    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RefreshAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToErrorResult(result);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<SpecialistMeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpecialistMeResponse>> GetMe(CancellationToken cancellationToken)
    {
        var specialistId = GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await authService.GetMeAsync(specialistId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToErrorResult(result);
    }

    private Guid? GetCurrentSpecialistId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var specialistId) ? specialistId : null;
    }

    private ActionResult<T> ToErrorResult<T>(ServiceResult<T> result)
    {
        var error = result.Error ?? new ServiceError("request_failed", "Request failed.");
        var response = new ErrorResponse(error.Code, error.Message);
        return result.Status switch
        {
            ResultStatus.Validation => BadRequest(response),
            ResultStatus.Conflict => Conflict(response),
            ResultStatus.Unauthorized => Unauthorized(response),
            ResultStatus.NotFound => NotFound(response),
            _ => BadRequest(response)
        };
    }
}
