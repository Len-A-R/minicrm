using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.Auth;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Infrastructure.Clients;

public sealed class ClientAuthRepository(ServiceBookingDbContext dbContext) : IClientAuthRepository
{
    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return dbContext.Clients.AnyAsync(client => client.Email == normalizedEmail, cancellationToken);
    }

    public Task<Client?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return dbContext.Clients.SingleOrDefaultAsync(client => client.Email == normalizedEmail, cancellationToken);
    }

    public Task<Client?> GetByPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        return dbContext.Clients.SingleOrDefaultAsync(client => client.Phone == phone, cancellationToken);
    }

    public Task<Client?> GetByIdAsync(Guid clientId, CancellationToken cancellationToken)
    {
        return dbContext.Clients.SingleOrDefaultAsync(client => client.Id == clientId, cancellationToken);
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken)
    {
        await dbContext.Clients.AddAsync(client, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
