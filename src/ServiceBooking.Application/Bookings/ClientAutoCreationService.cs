using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Bookings;

public sealed class ClientAutoCreationService(IClientAutoCreationRepository clients) : IClientAutoCreationService
{
    public async Task<Client> GetOrCreateAsync(
        string clientName,
        string clientPhone,
        CancellationToken cancellationToken)
    {
        var normalizedPhone = NormalizePhone(clientPhone);
        var client = await clients.GetByPhoneAsync(normalizedPhone, cancellationToken);
        if (client is not null)
        {
            client.Rename(clientName);
            return client;
        }

        client = new Client(clientName, normalizedPhone);
        await clients.AddAsync(client, cancellationToken);
        return client;
    }

    private static string NormalizePhone(string phone)
    {
        return phone.Trim();
    }
}
