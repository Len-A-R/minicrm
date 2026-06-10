using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.SpecialistServices;
using ServiceBooking.Infrastructure.Persistence;
using CatalogService = ServiceBooking.Domain.Entities.Service;
using DomainSpecialistService = ServiceBooking.Domain.Entities.SpecialistService;

namespace ServiceBooking.Infrastructure.SpecialistServices;

public sealed class SpecialistServiceRepository(ServiceBookingDbContext dbContext) : ISpecialistServiceRepository
{
    public async Task<IReadOnlyCollection<DomainSpecialistService>> ListForSpecialistAsync(
        Guid specialistId,
        CancellationToken cancellationToken)
    {
        return await dbContext.SpecialistServices
            .Include(specialistService => specialistService.Service)
            .Where(specialistService => specialistService.SpecialistId == specialistId)
            .OrderBy(specialistService => specialistService.Service!.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<DomainSpecialistService?> GetForSpecialistAsync(
        Guid specialistId,
        Guid specialistServiceId,
        CancellationToken cancellationToken)
    {
        return dbContext.SpecialistServices
            .Include(specialistService => specialistService.Service)
            .SingleOrDefaultAsync(
                specialistService => specialistService.SpecialistId == specialistId
                    && specialistService.Id == specialistServiceId,
                cancellationToken);
    }

    public Task<bool> SpecialistExistsAsync(Guid specialistId, CancellationToken cancellationToken)
    {
        return dbContext.Specialists.AnyAsync(specialist => specialist.Id == specialistId, cancellationToken);
    }

    public Task<CatalogService?> GetCatalogServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return dbContext.Services.SingleOrDefaultAsync(service => service.Id == serviceId, cancellationToken);
    }

    public Task<bool> HasDuplicateAsync(
        Guid specialistId,
        Guid serviceId,
        Guid? excludingSpecialistServiceId,
        CancellationToken cancellationToken)
    {
        return dbContext.SpecialistServices.AnyAsync(
            specialistService => specialistService.SpecialistId == specialistId
                && specialistService.ServiceId == serviceId
                && (!excludingSpecialistServiceId.HasValue || specialistService.Id != excludingSpecialistServiceId.Value),
            cancellationToken);
    }

    public async Task AddAsync(DomainSpecialistService specialistService, CancellationToken cancellationToken)
    {
        await dbContext.SpecialistServices.AddAsync(specialistService, cancellationToken);
    }

    public void Remove(DomainSpecialistService specialistService)
    {
        dbContext.SpecialistServices.Remove(specialistService);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
