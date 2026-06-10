using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.Bookings;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Infrastructure.Bookings;

public sealed class ClientAutoCreationRepository(ServiceBookingDbContext dbContext) : IClientAutoCreationRepository
{
    public Task<Client?> GetByPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        return dbContext.Clients.SingleOrDefaultAsync(client => client.Phone == phone, cancellationToken);
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken)
    {
        await dbContext.Clients.AddAsync(client, cancellationToken);
    }
}
