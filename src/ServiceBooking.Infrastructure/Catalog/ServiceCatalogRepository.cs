using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.Catalog;
using ServiceBooking.Infrastructure.Persistence;
using CatalogService = ServiceBooking.Domain.Entities.Service;

namespace ServiceBooking.Infrastructure.Catalog;

public sealed class ServiceCatalogRepository(ServiceBookingDbContext dbContext) : IServiceCatalogRepository
{
    public async Task<IReadOnlyCollection<CatalogService>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Services
            .OrderBy(service => service.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<CatalogService?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Services.SingleOrDefaultAsync(service => service.Id == id, cancellationToken);
    }

    public async Task AddAsync(CatalogService service, CancellationToken cancellationToken)
    {
        await dbContext.Services.AddAsync(service, cancellationToken);
    }

    public Task<bool> HasSpecialistServicesAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return dbContext.SpecialistServices.AnyAsync(
            specialistService => specialistService.ServiceId == serviceId,
            cancellationToken);
    }

    public void Remove(CatalogService service)
    {
        dbContext.Services.Remove(service);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
