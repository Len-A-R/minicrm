using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Kanban;

public interface IKanbanService
{
    Task<ServiceResult<KanbanBoardResponse>> GetBoardAsync(
        Guid specialistId,
        KanbanBoardQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<KanbanBookingCardResponse>> MoveAsync(
        Guid specialistId,
        Guid bookingId,
        MoveKanbanBookingRequest request,
        CancellationToken cancellationToken);
}
