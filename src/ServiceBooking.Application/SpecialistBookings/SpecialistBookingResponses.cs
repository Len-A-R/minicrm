using ServiceBooking.Application.Bookings;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Application.SpecialistBookings;

public sealed record SpecialistBookingResponse(
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
    DateTimeOffset CreatedAt,
    DateTimeOffset? ConfirmedAt,
    DateOnly? ConfirmedDate,
    TimeOnly? ConfirmedTime,
    DateTimeOffset? CompletedAt,
    decimal? ActualRevenue,
    string? RejectionReason,
    string? SpecialistReply,
    DateTimeOffset? RepliedAt);

public sealed record PagedBookingResponse(
    IReadOnlyCollection<SpecialistBookingResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
