using ServiceBooking.Application.Bookings;
using ServiceBooking.Application.Common;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Application.Calendar;

public sealed class CalendarService(ICalendarRepository calendarRepository) : ICalendarService
{
    public async Task<ServiceResult<IReadOnlyCollection<CalendarBookingResponse>>> ListAsync(
        Guid specialistId,
        CalendarRangeQuery query,
        CancellationToken cancellationToken)
    {
        if (specialistId == Guid.Empty)
        {
            return Validation<IReadOnlyCollection<CalendarBookingResponse>>(
                "invalid_specialist_id",
                "Specialist id is required.");
        }

        if (query.From > query.To)
        {
            return Validation<IReadOnlyCollection<CalendarBookingResponse>>(
                "invalid_range",
                "Range start cannot be later than range end.");
        }

        var bookings = await calendarRepository.ListScheduledAsync(
            specialistId,
            query.From,
            query.To,
            cancellationToken);

        return ServiceResult<IReadOnlyCollection<CalendarBookingResponse>>.Success(
            bookings
                .OrderBy(booking => ScheduledDate(booking))
                .ThenBy(booking => ScheduledTime(booking))
                .Select(ToResponse)
                .ToArray());
    }

    public async Task<ServiceResult<CalendarBookingResponse>> RescheduleAsync(
        Guid specialistId,
        Guid bookingId,
        RescheduleBookingRequest request,
        CancellationToken cancellationToken)
    {
        var bookingResult = await GetBookingOrFailureAsync(specialistId, bookingId, cancellationToken);
        if (!bookingResult.IsSuccess)
        {
            return FailureFromBookingResult(bookingResult);
        }

        var booking = bookingResult.Value!;
        if (booking.Status != BookingStatus.Confirmed)
        {
            return ServiceResult<CalendarBookingResponse>.Failure(
                ResultStatus.Conflict,
                "invalid_booking_status",
                "Only confirmed bookings can be rescheduled.");
        }

        var conflicts = await calendarRepository.GetBookingsForConflictCheckAsync(
            specialistId,
            request.Date,
            bookingId,
            cancellationToken);

        if (HasConflict(conflicts, request.Time, BookingDuration(booking)))
        {
            return ServiceResult<CalendarBookingResponse>.Failure(
                ResultStatus.Conflict,
                "slot_conflict",
                "Selected date and time conflict with another booking.");
        }

        booking.Confirm(request.Date, request.Time);
        await calendarRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<CalendarBookingResponse>.Success(ToResponse(booking));
    }

    public async Task<ServiceResult<bool>> CancelAsync(
        Guid specialistId,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var bookingResult = await GetBookingOrFailureAsync(specialistId, bookingId, cancellationToken);
        if (!bookingResult.IsSuccess)
        {
            return ServiceResult<bool>.Failure(
                bookingResult.Status,
                bookingResult.Error!.Code,
                bookingResult.Error.Message);
        }

        var booking = bookingResult.Value!;
        if (booking.Status != BookingStatus.Confirmed)
        {
            return ServiceResult<bool>.Failure(
                ResultStatus.Conflict,
                "invalid_booking_status",
                "Only confirmed bookings can be cancelled from calendar.");
        }

        booking.Reject("Cancelled from calendar.");
        await calendarRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    private async Task<ServiceResult<Booking>> GetBookingOrFailureAsync(
        Guid specialistId,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        if (specialistId == Guid.Empty || bookingId == Guid.Empty)
        {
            return Validation<Booking>("invalid_booking_id", "Booking id is required.");
        }

        var booking = await calendarRepository.GetByIdAsync(specialistId, bookingId, cancellationToken);
        return booking is null
            ? ServiceResult<Booking>.Failure(ResultStatus.NotFound, "booking_not_found", "Booking was not found.")
            : ServiceResult<Booking>.Success(booking);
    }

    private static bool HasConflict(IEnumerable<Booking> scheduledBookings, TimeOnly start, int durationMinutes)
    {
        var end = start.AddMinutes(durationMinutes);
        return scheduledBookings.Any(existing =>
        {
            var existingStart = ScheduledTime(existing);
            var existingEnd = existingStart.AddMinutes(BookingDuration(existing));
            return start < existingEnd && existingStart < end;
        });
    }

    private static DateOnly ScheduledDate(Booking booking) => booking.ConfirmedDate ?? booking.RequestedDate;

    private static TimeOnly ScheduledTime(Booking booking) => booking.ConfirmedTime ?? booking.RequestedTime;

    private static int BookingDuration(Booking booking) => Math.Max(booking.TotalDuration, 30);

    private static CalendarBookingResponse ToResponse(Booking booking)
    {
        var start = ScheduledTime(booking);
        var duration = BookingDuration(booking);
        return new CalendarBookingResponse(
            booking.Id,
            booking.ClientName,
            booking.ClientPhone,
            booking.Services
                .Select(service => new BookingServiceItemResponse(
                    service.ServiceId,
                    service.ServiceName,
                    service.Price,
                    service.DurationMinutes))
                .ToArray(),
            ScheduledDate(booking),
            start,
            start.AddMinutes(duration),
            duration,
            booking.TotalPrice,
            booking.Status,
            booking.Message);
    }

    private static ServiceResult<T> Validation<T>(string code, string message)
    {
        return ServiceResult<T>.Failure(ResultStatus.Validation, code, message);
    }

    private static ServiceResult<CalendarBookingResponse> FailureFromBookingResult(ServiceResult<Booking> result)
    {
        return ServiceResult<CalendarBookingResponse>.Failure(
            result.Status,
            result.Error!.Code,
            result.Error.Message);
    }
}
