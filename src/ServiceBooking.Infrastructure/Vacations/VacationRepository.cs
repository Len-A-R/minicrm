using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.Vacations;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Infrastructure.Vacations;

public sealed class VacationRepository(ServiceBookingDbContext dbContext) : IVacationRepository
{
    public async Task<IReadOnlyCollection<Vacation>> ListForSpecialistAsync(
        Guid specialistId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Vacations
            .Where(vacation => vacation.SpecialistId == specialistId)
            .OrderBy(vacation => vacation.Date)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Vacation?> GetForSpecialistAsync(
        Guid specialistId,
        Guid vacationId,
        CancellationToken cancellationToken)
    {
        return dbContext.Vacations.SingleOrDefaultAsync(
            vacation => vacation.SpecialistId == specialistId && vacation.Id == vacationId,
            cancellationToken);
    }

    public Task<bool> SpecialistExistsAsync(Guid specialistId, CancellationToken cancellationToken)
    {
        return dbContext.Specialists.AnyAsync(specialist => specialist.Id == specialistId, cancellationToken);
    }

    public Task<bool> HasDuplicateDateAsync(
        Guid specialistId,
        DateOnly date,
        Guid? excludingVacationId,
        CancellationToken cancellationToken)
    {
        return dbContext.Vacations.AnyAsync(
            vacation => vacation.SpecialistId == specialistId
                && vacation.Date == date
                && (!excludingVacationId.HasValue || vacation.Id != excludingVacationId.Value),
            cancellationToken);
    }

    public async Task AddAsync(Vacation vacation, CancellationToken cancellationToken)
    {
        await dbContext.Vacations.AddAsync(vacation, cancellationToken);
    }

    public void Remove(Vacation vacation)
    {
        dbContext.Vacations.Remove(vacation);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
