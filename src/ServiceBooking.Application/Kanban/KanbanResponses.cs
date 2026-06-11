using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Application.Kanban;

public sealed record KanbanBoardResponse(
    DateOnly Date,
    IReadOnlyCollection<KanbanColumnResponse> Columns);

public sealed record KanbanColumnResponse(
    BookingStatus Status,
    IReadOnlyCollection<KanbanBookingCardResponse> Items);

public sealed record KanbanBookingCardResponse(
    Guid Id,
    string ClientName,
    string ClientPhone,
    DateOnly Date,
    TimeOnly Time,
    string ServicesSummary,
    decimal TotalPrice,
    int TotalDuration,
    string? Message);
