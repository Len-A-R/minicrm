using System.Net.Mail;
using ServiceBooking.Application.Common;
using ServiceBooking.Application.Specialists;
using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Auth;

public sealed class AuthService(
    ISpecialistRepository specialists,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IDateTimeProvider dateTimeProvider) : IAuthService
{
    public async Task<ServiceResult<AuthResponse>> RegisterAsync(
        RegisterSpecialistRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRegistration(request);
        if (validationError is not null)
        {
            return ServiceResult<AuthResponse>.Failure(ResultStatus.Validation, validationError.Code, validationError.Message);
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        if (await specialists.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return ServiceResult<AuthResponse>.Failure(
                ResultStatus.Conflict,
                "email_conflict",
                "A specialist with this email already exists.");
        }

        var specialist = new Specialist(
            request.FullName,
            normalizedEmail,
            request.Phone,
            passwordHasher.Hash(request.Password));

        await specialists.AddAsync(specialist, cancellationToken);
        return await IssueTokensAsync(specialist, cancellationToken);
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (!IsValidEmail(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return InvalidCredentials();
        }

        var specialist = await specialists.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken);
        if (specialist is null || !passwordHasher.Verify(request.Password, specialist.PasswordHash))
        {
            return InvalidCredentials();
        }

        return await IssueTokensAsync(specialist, cancellationToken);
    }

    public async Task<ServiceResult<AuthResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SpecialistId == Guid.Empty || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return ServiceResult<AuthResponse>.Failure(
                ResultStatus.Validation,
                "invalid_refresh_request",
                "Specialist id and refresh token are required.");
        }

        var specialist = await specialists.GetByIdAsync(request.SpecialistId, cancellationToken);
        if (specialist is null
            || !specialist.HasActiveRefreshToken(dateTimeProvider.UtcNow)
            || specialist.RefreshTokenHash is null
            || !passwordHasher.Verify(request.RefreshToken, specialist.RefreshTokenHash))
        {
            return ServiceResult<AuthResponse>.Failure(
                ResultStatus.Unauthorized,
                "invalid_refresh_token",
                "Refresh token is invalid or expired.");
        }

        return await IssueTokensAsync(specialist, cancellationToken);
    }

    public async Task<ServiceResult<SpecialistMeResponse>> GetMeAsync(
        Guid specialistId,
        CancellationToken cancellationToken)
    {
        var specialist = await specialists.GetByIdAsync(specialistId, cancellationToken);
        if (specialist is null)
        {
            return ServiceResult<SpecialistMeResponse>.Failure(
                ResultStatus.NotFound,
                "specialist_not_found",
                "Specialist was not found.");
        }

        return ServiceResult<SpecialistMeResponse>.Success(new SpecialistMeResponse(
            specialist.Id,
            specialist.FullName,
            specialist.Email,
            specialist.Phone,
            specialist.AvatarUrl,
            specialist.VenueName,
            specialist.LocationId));
    }

    private async Task<ServiceResult<AuthResponse>> IssueTokensAsync(
        Specialist specialist,
        CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;
        var accessToken = tokenService.CreateAccessToken(specialist, utcNow);
        var refreshToken = tokenService.CreateRefreshToken(utcNow);

        specialist.SetRefreshToken(passwordHasher.Hash(refreshToken.Token), refreshToken.ExpiresAt);
        await specialists.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthResponse>.Success(new AuthResponse(
            specialist.Id,
            specialist.FullName,
            specialist.Email,
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt));
    }

    private static ServiceResult<AuthResponse> InvalidCredentials()
    {
        return ServiceResult<AuthResponse>.Failure(
            ResultStatus.Unauthorized,
            "invalid_credentials",
            "Email or password is invalid.");
    }

    private static ServiceError? ValidateRegistration(RegisterSpecialistRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length < 2)
        {
            return new ServiceError("invalid_full_name", "Full name must contain at least 2 characters.");
        }

        if (!IsValidEmail(request.Email))
        {
            return new ServiceError("invalid_email", "Email format is invalid.");
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return new ServiceError("invalid_phone", "Phone is required.");
        }

        if (request.Password != request.ConfirmPassword)
        {
            return new ServiceError("password_mismatch", "Password confirmation does not match.");
        }

        if (!IsStrongPassword(request.Password))
        {
            return new ServiceError(
                "weak_password",
                "Password must contain at least 8 characters, uppercase, lowercase and digit.");
        }

        return null;
    }

    private static bool IsStrongPassword(string password)
    {
        return password.Length >= 8
            && password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit);
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
