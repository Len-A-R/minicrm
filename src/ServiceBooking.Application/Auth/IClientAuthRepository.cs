using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Auth;

public interface IClientAuthRepository
{
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<Client?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<Client?> GetByPhoneAsync(string phone, CancellationToken cancellationToken);

    Task<Client?> GetByIdAsync(Guid clientId, CancellationToken cancellationToken);

    Task AddAsync(Client client, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
