using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Common;
using ServiceBooking.Application.Profile;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/profile")]
public sealed class SpecialistProfileController(IProfileService profileService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> GetProfile(CancellationToken cancellationToken)
    {
        var specialistId = GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await profileService.GetProfileAsync(specialistId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToErrorResult(result);
    }

    [HttpPut]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> UpdateProfile(
        UpdateSpecialistProfileRequest request,
        CancellationToken cancellationToken)
    {
        var specialistId = GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await profileService.UpdateProfileAsync(specialistId.Value, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToErrorResult(result);
    }

    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> UploadAvatar(
        IFormFile avatar,
        CancellationToken cancellationToken)
    {
        var specialistId = GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        if (avatar is null)
        {
            return BadRequest(new ErrorResponse("avatar_required", "Avatar file is required."));
        }

        await using var stream = avatar.OpenReadStream();
        var request = new AvatarUploadRequest(stream, avatar.FileName, avatar.ContentType, avatar.Length);
        var result = await profileService.UploadAvatarAsync(specialistId.Value, request, cancellationToken);
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
