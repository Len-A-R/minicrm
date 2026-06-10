using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Vacations;

public interface IVacationRepository
{
    Task<IReadOnlyCollection<Vacation>> ListForSpecialistAsync(Guid specialistId, CancellationToken cancellationToken);

    Task<Vacation?> GetForSpecialistAsync(Guid specialistId, Guid vacationId, CancellationToken cancellationToken);

    Task<bool> SpecialistExistsAsync(Guid specialistId, CancellationToken cancellationToken);

    Task<bool> HasDuplicateDateAsync(
        Guid specialistId,
        DateOnly date,
        Guid? excludingVacationId,
        CancellationToken cancellationToken);

    Task AddAsync(Vacation vacation, CancellationToken cancellationToken);

    void Remove(Vacation vacation);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
