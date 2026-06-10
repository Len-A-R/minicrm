using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Application.Bookings;

public sealed record BookingResponse(
    Guid Id,
    string ClientName,
    string ClientPhone,
    Guid SpecialistId,
    Guid? ClientId,
    IReadOnlyCollection<BookingServiceItemResponse> Services,
    DateOnly RequestedDate,
    TimeOnly RequestedTime,
    string? Message,
    decimal TotalPrice,
    int TotalDuration,
    BookingStatus Status,
    DateTimeOffset CreatedAt);
