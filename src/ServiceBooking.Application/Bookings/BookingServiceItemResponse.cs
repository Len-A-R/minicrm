namespace ServiceBooking.Application.Bookings;

public sealed record BookingServiceItemResponse(
    Guid ServiceId,
    string ServiceName,
    decimal Price,
    int DurationMinutes);
