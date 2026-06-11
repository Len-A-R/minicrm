using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Auth;

public interface IAdminAuthLookupRepository
{
    Task<AdminUser?> GetAdminByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<bool> AdminEmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
