using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.Reports;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Infrastructure.Reports;

public sealed class ReportRepository(ServiceBookingDbContext dbContext) : IReportRepository
{
    public async Task<IReadOnlyCollection<Booking>> ListCompletedAsync(Guid specialistId, CancellationToken cancellationToken)
    {
        return await dbContext.Bookings
            .Include(booking => booking.Services)
            .Where(booking => booking.SpecialistId == specialistId && booking.Status == BookingStatus.Completed)
            .ToArrayAsync(cancellationToken);
    }
}
