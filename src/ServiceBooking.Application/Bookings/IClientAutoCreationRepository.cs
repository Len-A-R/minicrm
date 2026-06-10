using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Bookings;

public interface IClientAutoCreationRepository
{
    Task<Client?> GetByPhoneAsync(string phone, CancellationToken cancellationToken);

    Task AddAsync(Client client, CancellationToken cancellationToken);
}
