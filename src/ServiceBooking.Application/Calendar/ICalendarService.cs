using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Calendar;

public interface ICalendarService
{
    Task<ServiceResult<IReadOnlyCollection<CalendarBookingResponse>>> ListAsync(
        Guid specialistId,
        CalendarRangeQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<CalendarBookingResponse>> RescheduleAsync(
        Guid specialistId,
        Guid bookingId,
        RescheduleBookingRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> CancelAsync(
        Guid specialistId,
        Guid bookingId,
        CancellationToken cancellationToken);
}
