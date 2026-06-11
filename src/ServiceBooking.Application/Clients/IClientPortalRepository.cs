using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Clients;

public interface IClientPortalRepository
{
    Task<Client?> GetClientByIdAsync(Guid clientId, CancellationToken cancellationToken);

    Task<bool> PhoneExistsAsync(string phone, Guid excludingClientId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Booking>> ListBookingsAsync(Guid clientId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
