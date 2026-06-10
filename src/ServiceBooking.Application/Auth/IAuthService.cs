using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Auth;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterSpecialistRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<SpecialistMeResponse>> GetMeAsync(Guid specialistId, CancellationToken cancellationToken);
}
