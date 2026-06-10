using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.SpecialistBookings;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Infrastructure.Bookings;

public sealed class BookingActionRepository(ServiceBookingDbContext dbContext) : IBookingActionRepository
{
    public async Task<(IReadOnlyCollection<Booking> Items, int TotalCount)> ListAsync(
        Guid specialistId,
        BookingStatus? status,
        DateOnly? date,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = BaseQuery()
            .Where(booking => booking.SpecialistId == specialistId);

        if (status.HasValue)
        {
            query = query.Where(booking => booking.Status == status.Value);
        }

        if (date.HasValue)
        {
            query = query.Where(booking => booking.RequestedDate == date.Value
                || booking.ConfirmedDate == date.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim();
            query = query.Where(booking => booking.ClientName.Contains(normalized)
                || booking.ClientPhone.Contains(normalized));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var filteredItems = await query.ToArrayAsync(cancellationToken);
        var items = filteredItems
            .OrderByDescending(booking => booking.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return (items, totalCount);
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
                && (booking.Status == BookingStatus.Confirmed || booking.Status == BookingStatus.Completed)
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
