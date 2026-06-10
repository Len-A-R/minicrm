namespace ServiceBooking.Application.Slots;

public sealed record AvailableSlotResponse(DateOnly Date, TimeOnly Time, int DurationMinutes);
