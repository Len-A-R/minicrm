using ServiceBooking.Domain.Entities;
using DomainSpecialistService = ServiceBooking.Domain.Entities.SpecialistService;

namespace ServiceBooking.Application.SpecialistServices;

public interface ISpecialistServiceRepository
{
    Task<IReadOnlyCollection<DomainSpecialistService>> ListForSpecialistAsync(
        Guid specialistId,
        CancellationToken cancellationToken);

    Task<DomainSpecialistService?> GetForSpecialistAsync(
        Guid specialistId,
        Guid specialistServiceId,
        CancellationToken cancellationToken);

    Task<bool> SpecialistExistsAsync(Guid specialistId, CancellationToken cancellationToken);

    Task<Service?> GetCatalogServiceAsync(Guid serviceId, CancellationToken cancellationToken);

    Task<bool> HasDuplicateAsync(
        Guid specialistId,
        Guid serviceId,
        Guid? excludingSpecialistServiceId,
        CancellationToken cancellationToken);

    Task AddAsync(DomainSpecialistService specialistService, CancellationToken cancellationToken);

    void Remove(DomainSpecialistService specialistService);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
