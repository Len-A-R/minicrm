using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Common;

namespace ServiceBooking.API.Controllers;

[ExcludeFromCodeCoverage]
internal static class ControllerResultExtensions
{
    public static Guid? GetCurrentSpecialistId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var specialistId) ? specialistId : null;
    }

    public static Guid? GetCurrentUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var userId) ? userId : null;
    }

    public static ActionResult<T> ToErrorResult<T>(this ControllerBase controller, ServiceResult<T> result)
    {
        var error = result.Error ?? new ServiceError("request_failed", "Request failed.");
        var response = new ErrorResponse(error.Code, error.Message);
        return result.Status switch
        {
            ResultStatus.Validation => controller.BadRequest(response),
            ResultStatus.Conflict => controller.Conflict(response),
            ResultStatus.Unauthorized => controller.Unauthorized(response),
            ResultStatus.NotFound => controller.NotFound(response),
            _ => controller.BadRequest(response)
        };
    }

    public static IActionResult ToBooleanErrorResult(this ControllerBase controller, ServiceResult<bool> result)
    {
        var error = result.Error ?? new ServiceError("request_failed", "Request failed.");
        var response = new ErrorResponse(error.Code, error.Message);
        return result.Status switch
        {
            ResultStatus.Validation => controller.BadRequest(response),
            ResultStatus.Conflict => controller.Conflict(response),
            ResultStatus.Unauthorized => controller.Unauthorized(response),
            ResultStatus.NotFound => controller.NotFound(response),
            _ => controller.BadRequest(response)
        };
    }
}
