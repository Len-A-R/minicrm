using ServiceBooking.Application.Bookings;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Application.Calendar;

public sealed record CalendarBookingResponse(
    Guid Id,
    string ClientName,
    string ClientPhone,
    IReadOnlyCollection<BookingServiceItemResponse> Services,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int DurationMinutes,
    decimal TotalPrice,
    BookingStatus Status,
    string? Message);
