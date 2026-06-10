namespace ServiceBooking.Application.Bookings;

public sealed record CreateBookingRequest(
    string ClientName,
    string ClientPhone,
    Guid SpecialistId,
    IReadOnlyCollection<Guid> ServiceIds,
    DateOnly RequestedDate,
    TimeOnly RequestedTime,
    string? Message);
