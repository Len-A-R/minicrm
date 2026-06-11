using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.Specialists;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Infrastructure.Specialists;

public sealed class PublicSpecialistRepository(ServiceBookingDbContext dbContext) : IPublicSpecialistRepository
{
    public async Task<IReadOnlyCollection<Specialist>> ListAsync(
        Guid locationId,
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Specialists
            .Include(specialist => specialist.Location)
            .Include(specialist => specialist.Services)
            .Where(specialist => specialist.LocationId == locationId
                && !specialist.IsBlocked
                && specialist.Services.Any(specialistService => specialistService.ServiceId == serviceId))
            .OrderBy(specialist => specialist.FullName)
            .ToArrayAsync(cancellationToken);
    }
}
