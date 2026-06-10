using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Application.SpecialistClients;

public sealed record SpecialistClientResponse(
    Guid Id,
    string FullName,
    string Phone,
    ClientStatus Status,
    string? Tag,
    int BookingCount,
    DateTimeOffset? LastBookingAt);
