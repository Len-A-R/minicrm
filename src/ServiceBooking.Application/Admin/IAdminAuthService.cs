using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Admin;

public interface IAdminAuthService
{
    Task<ServiceResult<AdminAuthResponse>> LoginAsync(AdminLoginRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<AdminMeResponse>> GetMeAsync(Guid adminId, CancellationToken cancellationToken);
}
