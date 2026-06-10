using CatalogService = ServiceBooking.Domain.Entities.Service;

namespace ServiceBooking.Application.Catalog;

public interface IServiceCatalogRepository
{
    Task<IReadOnlyCollection<CatalogService>> ListAsync(CancellationToken cancellationToken);

    Task<CatalogService?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(CatalogService service, CancellationToken cancellationToken);

    Task<bool> HasSpecialistServicesAsync(Guid serviceId, CancellationToken cancellationToken);

    void Remove(CatalogService service);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
