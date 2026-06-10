using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.SpecialistClients;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Infrastructure.Clients;

public sealed class SpecialistClientRepository(ServiceBookingDbContext dbContext) : ISpecialistClientRepository
{
    public async Task<IReadOnlyCollection<Client>> ListAsync(Guid specialistId, CancellationToken cancellationToken)
    {
        return await BaseQuery(specialistId)
            .OrderBy(client => client.FullName)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Client?> GetByIdAsync(Guid specialistId, Guid clientId, CancellationToken cancellationToken)
    {
        return BaseQuery(specialistId)
            .SingleOrDefaultAsync(client => client.Id == clientId, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Client> BaseQuery(Guid specialistId)
    {
        return dbContext.Clients
            .Include(client => client.Bookings.Where(booking => booking.SpecialistId == specialistId))
            .Where(client => client.Bookings.Any(booking => booking.SpecialistId == specialistId));
    }
}
