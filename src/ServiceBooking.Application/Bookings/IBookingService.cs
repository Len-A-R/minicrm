using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Bookings;

public interface IBookingService
{
    Task<ServiceResult<BookingResponse>> CreateAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken);
}
