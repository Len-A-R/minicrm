using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.Specialists;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Infrastructure.Specialists;

public sealed class SpecialistRepository(ServiceBookingDbContext dbContext) : ISpecialistRepository
{
    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return dbContext.Specialists.AnyAsync(
            specialist => specialist.Email == normalizedEmail,
            cancellationToken);
    }

    public Task<Specialist?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return dbContext.Specialists.SingleOrDefaultAsync(
            specialist => specialist.Email == normalizedEmail,
            cancellationToken);
    }

    public Task<Specialist?> GetByIdAsync(Guid specialistId, CancellationToken cancellationToken)
    {
        return dbContext.Specialists.SingleOrDefaultAsync(
            specialist => specialist.Id == specialistId,
            cancellationToken);
    }

    public async Task AddAsync(Specialist specialist, CancellationToken cancellationToken)
    {
        await dbContext.Specialists.AddAsync(specialist, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
