using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Bookings;

public interface IClientAutoCreationService
{
    Task<Client> GetOrCreateAsync(string clientName, string clientPhone, CancellationToken cancellationToken);
}
