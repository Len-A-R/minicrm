using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.Clients;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Infrastructure.Clients;

public sealed class ClientPortalRepository(ServiceBookingDbContext dbContext) : IClientPortalRepository
{
    public Task<Client?> GetClientByIdAsync(Guid clientId, CancellationToken cancellationToken)
    {
        return dbContext.Clients.SingleOrDefaultAsync(client => client.Id == clientId, cancellationToken);
    }

    public Task<bool> PhoneExistsAsync(string phone, Guid excludingClientId, CancellationToken cancellationToken)
    {
        return dbContext.Clients.AnyAsync(
            client => client.Phone == phone && client.Id != excludingClientId,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<Booking>> ListBookingsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        return await dbContext.Bookings
            .Include(booking => booking.Specialist)
                .ThenInclude(specialist => specialist!.Location)
            .Include(booking => booking.Services)
            .Where(booking => booking.ClientId == clientId)
            .ToArrayAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
