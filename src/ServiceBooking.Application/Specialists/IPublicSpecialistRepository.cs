using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Specialists;

public interface IPublicSpecialistRepository
{
    Task<IReadOnlyCollection<Specialist>> ListAsync(
        Guid locationId,
        Guid serviceId,
        CancellationToken cancellationToken);
}
