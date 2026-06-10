using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Bookings;

public interface IBookingRepository
{
    Task<bool> SpecialistExistsAsync(Guid specialistId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SpecialistServiceBookingOption>> GetSpecialistServicesAsync(
        Guid specialistId,
        IReadOnlyCollection<Guid> serviceIds,
        CancellationToken cancellationToken);

    Task AddAsync(Booking booking, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
