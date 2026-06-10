using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Bookings;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Route("api/v1/bookings")]
public sealed class BookingsController(IBookingService bookingService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponse>> Create(
        CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await bookingService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToErrorResult(result);
        }

        return Created($"api/v1/bookings/{result.Value!.Id}", result.Value);
    }
}
