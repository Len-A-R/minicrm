using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Application.Kanban;

public sealed record KanbanBoardQuery(DateOnly Date);

public sealed record MoveKanbanBookingRequest(BookingStatus Status);
