namespace ServiceBooking.Application.Calendar;

public sealed record CalendarRangeQuery(DateOnly From, DateOnly To);

public sealed record RescheduleBookingRequest(DateOnly Date, TimeOnly Time);
