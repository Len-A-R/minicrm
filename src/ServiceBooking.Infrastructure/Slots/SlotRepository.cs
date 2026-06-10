using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.Slots;
using ServiceBooking.Domain.Enums;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Infrastructure.Slots;

public sealed class SlotRepository(ServiceBookingDbContext dbContext) : ISlotRepository
{
    public Task<bool> SpecialistExistsAsync(Guid specialistId, CancellationToken cancellationToken)
    {
        return dbContext.Specialists.AnyAsync(specialist => specialist.Id == specialistId, cancellationToken);
    }

    public Task<bool> IsVacationDateAsync(Guid specialistId, DateOnly date, CancellationToken cancellationToken)
    {
        return dbContext.Vacations.AnyAsync(
            vacation => vacation.SpecialistId == specialistId && vacation.Date == date,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<SlotBookingSnapshot>> GetConfirmedBookingsAsync(
        Guid specialistId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        return await dbContext.Bookings
            .Where(booking => booking.SpecialistId == specialistId
                && booking.Status == BookingStatus.Confirmed
                && (booking.ConfirmedDate ?? booking.RequestedDate) == date)
            .Select(booking => new SlotBookingSnapshot(
                booking.ConfirmedDate ?? booking.RequestedDate,
                booking.ConfirmedTime ?? booking.RequestedTime,
                booking.TotalDuration))
            .ToArrayAsync(cancellationToken);
    }
}
