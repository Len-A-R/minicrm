using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Bookings;
using ServiceBooking.Application.Clients;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Authorize(Roles = "Client")]
[Route("api/v1/client")]
public sealed class ClientPortalController(IClientPortalService clientPortal) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<ClientMeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientMeResponse>> Me(CancellationToken cancellationToken)
    {
        var clientId = User.GetCurrentUserId();
        if (clientId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain client id."));
        }

        var result = await clientPortal.GetMeAsync(clientId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPut("me")]
    [ProducesResponseType<ClientMeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientMeResponse>> UpdateMe(
        UpdateClientProfileRequest request,
        CancellationToken cancellationToken)
    {
        var clientId = User.GetCurrentUserId();
        if (clientId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain client id."));
        }

        var result = await clientPortal.UpdateMeAsync(clientId.Value, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPost("bookings")]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponse>> CreateBooking(
        CreateClientBookingRequest request,
        CancellationToken cancellationToken)
    {
        var clientId = User.GetCurrentUserId();
        if (clientId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain client id."));
        }

        var result = await clientPortal.CreateBookingAsync(clientId.Value, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToErrorResult(result);
        }

        return Created($"api/v1/client/bookings/{result.Value!.Id}", result.Value);
    }

    [HttpGet("bookings")]
    [ProducesResponseType<IReadOnlyCollection<ClientBookingHistoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<ClientBookingHistoryResponse>>> ListBookings(
        CancellationToken cancellationToken)
    {
        var clientId = User.GetCurrentUserId();
        if (clientId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain client id."));
        }

        var result = await clientPortal.ListBookingsAsync(clientId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpGet("notifications")]
    [ProducesResponseType<IReadOnlyCollection<ClientNotificationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<ClientNotificationResponse>>> ListNotifications(
        CancellationToken cancellationToken)
    {
        var clientId = User.GetCurrentUserId();
        if (clientId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain client id."));
        }

        var result = await clientPortal.ListNotificationsAsync(clientId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }
}
