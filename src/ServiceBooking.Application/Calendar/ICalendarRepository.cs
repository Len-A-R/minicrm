using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Calendar;

public interface ICalendarRepository
{
    Task<IReadOnlyCollection<Booking>> ListScheduledAsync(
        Guid specialistId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    Task<Booking?> GetByIdAsync(Guid specialistId, Guid bookingId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Booking>> GetBookingsForConflictCheckAsync(
        Guid specialistId,
        DateOnly date,
        Guid excludedBookingId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
