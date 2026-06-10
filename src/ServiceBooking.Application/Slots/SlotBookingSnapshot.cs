namespace ServiceBooking.Application.Slots;

public sealed record SlotBookingSnapshot(DateOnly Date, TimeOnly Time, int DurationMinutes);
