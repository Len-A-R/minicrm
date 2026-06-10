using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.SpecialistClients;

public interface ISpecialistClientRepository
{
    Task<IReadOnlyCollection<Client>> ListAsync(Guid specialistId, CancellationToken cancellationToken);

    Task<Client?> GetByIdAsync(Guid specialistId, Guid clientId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
