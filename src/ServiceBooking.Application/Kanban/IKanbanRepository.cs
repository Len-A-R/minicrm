using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Kanban;

public interface IKanbanRepository
{
    Task<IReadOnlyCollection<Booking>> ListByDateAsync(
        Guid specialistId,
        DateOnly date,
        CancellationToken cancellationToken);

    Task<Booking?> GetByIdAsync(Guid specialistId, Guid bookingId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Booking>> GetBookingsForConflictCheckAsync(
        Guid specialistId,
        DateOnly date,
        Guid excludedBookingId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
