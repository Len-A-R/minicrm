using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.Bookings;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Infrastructure.Bookings;

public sealed class BookingRepository(ServiceBookingDbContext dbContext) : IBookingRepository
{
    public Task<bool> SpecialistExistsAsync(Guid specialistId, CancellationToken cancellationToken)
    {
        return dbContext.Specialists.AnyAsync(specialist => specialist.Id == specialistId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<SpecialistServiceBookingOption>> GetSpecialistServicesAsync(
        Guid specialistId,
        IReadOnlyCollection<Guid> serviceIds,
        CancellationToken cancellationToken)
    {
        if (serviceIds.Count == 0)
        {
            return [];
        }

        return await dbContext.SpecialistServices
            .Include(specialistService => specialistService.Service)
            .Where(specialistService => specialistService.SpecialistId == specialistId
                && serviceIds.Contains(specialistService.ServiceId))
            .Select(specialistService => new SpecialistServiceBookingOption(
                specialistService.ServiceId,
                specialistService.Service!.Name,
                specialistService.Price,
                specialistService.DurationMinutes))
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken)
    {
        await dbContext.Bookings.AddAsync(booking, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
