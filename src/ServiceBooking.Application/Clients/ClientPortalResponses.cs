using ServiceBooking.Application.Bookings;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Application.Clients;

public sealed record ClientMeResponse(
    Guid ClientId,
    string FullName,
    string Email,
    string Phone);

public sealed record ClientBookingHistoryResponse(
    Guid Id,
    Guid SpecialistId,
    string SpecialistName,
    Guid? SpecialistLocationId,
    string? SpecialistLocationName,
    IReadOnlyCollection<BookingServiceItemResponse> Services,
    DateOnly RequestedDate,
    TimeOnly RequestedTime,
    DateOnly? ConfirmedDate,
    TimeOnly? ConfirmedTime,
    decimal TotalPrice,
    int TotalDuration,
    BookingStatus Status,
    string? Message,
    string? RejectionReason,
    string? SpecialistReply,
    DateTimeOffset? RepliedAt,
    DateTimeOffset CreatedAt);

public sealed record ClientNotificationResponse(
    Guid BookingId,
    string SpecialistName,
    string Reply,
    DateTimeOffset RepliedAt);
