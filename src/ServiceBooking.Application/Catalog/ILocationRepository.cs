using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Catalog;

public interface ILocationRepository
{
    Task<IReadOnlyCollection<Location>> ListAsync(Guid? serviceId, CancellationToken cancellationToken);

    Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Location location, CancellationToken cancellationToken);

    void Remove(Location location);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
