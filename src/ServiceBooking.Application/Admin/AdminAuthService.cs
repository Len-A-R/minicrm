using System.Net.Mail;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Admin;

public sealed class AdminAuthService(
    IAdminRepository repository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IDateTimeProvider dateTimeProvider) : IAdminAuthService
{
    private const string DefaultAdminEmail = "admin@minicrm";

    public async Task<ServiceResult<AdminAuthResponse>> LoginAsync(AdminLoginRequest request, CancellationToken cancellationToken)
    {
        if (!IsValidEmail(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return InvalidCredentials();
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        if (normalizedEmail != DefaultAdminEmail)
        {
            return InvalidCredentials();
        }

        var admin = await repository.GetAdminByEmailAsync(normalizedEmail, cancellationToken);
        if (admin is null || !admin.IsActive || !passwordHasher.Verify(request.Password, admin.PasswordHash))
        {
            return InvalidCredentials();
        }

        var utcNow = dateTimeProvider.UtcNow;
        var token = tokenService.CreateAdminAccessToken(admin, utcNow);
        admin.RecordLogin(utcNow);
        await repository.SaveChangesAsync(cancellationToken);

        return ServiceResult<AdminAuthResponse>.Success(new AdminAuthResponse(
            admin.Id,
            admin.FullName,
            admin.Email,
            token.Token,
            token.ExpiresAt));
    }

    public async Task<ServiceResult<AdminMeResponse>> GetMeAsync(Guid adminId, CancellationToken cancellationToken)
    {
        var admin = await repository.GetAdminByIdAsync(adminId, cancellationToken);
        if (admin is null)
        {
            return ServiceResult<AdminMeResponse>.Failure(ResultStatus.NotFound, "admin_not_found", "Admin was not found.");
        }

        return ServiceResult<AdminMeResponse>.Success(new AdminMeResponse(
            admin.Id,
            admin.FullName,
            admin.Email,
            admin.IsActive));
    }

    private static ServiceResult<AdminAuthResponse> InvalidCredentials()
    {
        return ServiceResult<AdminAuthResponse>.Failure(
            ResultStatus.Unauthorized,
            "invalid_admin_credentials",
            "Admin email or password is invalid.");
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var address = new MailAddress(email.Trim());
            return string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
