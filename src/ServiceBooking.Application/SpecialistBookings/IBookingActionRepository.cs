using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Application.SpecialistBookings;

public interface IBookingActionRepository
{
    Task<(IReadOnlyCollection<Booking> Items, int TotalCount)> ListAsync(
        Guid specialistId,
        BookingStatus? status,
        DateOnly? date,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<Booking?> GetByIdAsync(Guid specialistId, Guid bookingId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Booking>> GetBookingsForConflictCheckAsync(
        Guid specialistId,
        DateOnly date,
        Guid excludedBookingId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
