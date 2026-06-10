using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.Catalog;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Infrastructure.Catalog;

public sealed class LocationRepository(ServiceBookingDbContext dbContext) : ILocationRepository
{
    public async Task<IReadOnlyCollection<Location>> ListAsync(Guid? serviceId, CancellationToken cancellationToken)
    {
        var query = dbContext.Locations.AsQueryable();

        if (serviceId.HasValue)
        {
            query = query.Where(location => location.Specialists.Any(specialist =>
                specialist.Services.Any(specialistService => specialistService.ServiceId == serviceId.Value)));
        }

        return await query
            .OrderBy(location => location.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Locations.SingleOrDefaultAsync(location => location.Id == id, cancellationToken);
    }

    public async Task AddAsync(Location location, CancellationToken cancellationToken)
    {
        await dbContext.Locations.AddAsync(location, cancellationToken);
    }

    public void Remove(Location location)
    {
        dbContext.Locations.Remove(location);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
