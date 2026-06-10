namespace ServiceBooking.Application.Bookings;

public sealed record SpecialistServiceBookingOption(
    Guid ServiceId,
    string ServiceName,
    decimal Price,
    int DurationMinutes);
