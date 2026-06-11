using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.Calendar;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Infrastructure.Calendar;

public sealed class CalendarRepository(ServiceBookingDbContext dbContext) : ICalendarRepository
{
    public async Task<IReadOnlyCollection<Booking>> ListScheduledAsync(
        Guid specialistId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        return await BaseQuery()
            .Where(booking => booking.SpecialistId == specialistId
                && (booking.Status == BookingStatus.Confirmed || booking.Status == BookingStatus.Completed)
                && (booking.ConfirmedDate ?? booking.RequestedDate) >= from
                && (booking.ConfirmedDate ?? booking.RequestedDate) <= to)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Booking?> GetByIdAsync(Guid specialistId, Guid bookingId, CancellationToken cancellationToken)
    {
        return BaseQuery()
            .SingleOrDefaultAsync(
                booking => booking.SpecialistId == specialistId && booking.Id == bookingId,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<Booking>> GetBookingsForConflictCheckAsync(
        Guid specialistId,
        DateOnly date,
        Guid excludedBookingId,
        CancellationToken cancellationToken)
    {
        return await BaseQuery()
            .Where(booking => booking.SpecialistId == specialistId
                && booking.Id != excludedBookingId
                && booking.Status == BookingStatus.Confirmed
                && (booking.ConfirmedDate ?? booking.RequestedDate) == date)
            .ToArrayAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Booking> BaseQuery()
    {
        return dbContext.Bookings.Include(booking => booking.Services);
    }
}
