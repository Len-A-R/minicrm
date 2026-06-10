using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Specialists;

public interface ISpecialistRepository
{
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<Specialist?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<Specialist?> GetByIdAsync(Guid specialistId, CancellationToken cancellationToken);

    Task AddAsync(Specialist specialist, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
