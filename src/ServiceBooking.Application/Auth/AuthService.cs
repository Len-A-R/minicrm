using System.Net.Mail;
using ServiceBooking.Application.Common;
using ServiceBooking.Application.Specialists;
using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Auth;

public sealed class AuthService(
    ISpecialistRepository specialists,
    IClientAuthRepository clients,
    IAdminAuthLookupRepository admins,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IDateTimeProvider dateTimeProvider) : IAuthService
{
    private const string DefaultAdminEmail = "admin@minicrm";

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
        if (await EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return ServiceResult<AuthResponse>.Failure(
                ResultStatus.Conflict,
                "email_conflict",
                "An account with this email already exists.");
        }

        var specialist = new Specialist(
            request.FullName,
            normalizedEmail,
            request.Phone,
            passwordHasher.Hash(request.Password));

        await specialists.AddAsync(specialist, cancellationToken);
        return await IssueTokensAsync(specialist, cancellationToken);
    }

    public async Task<ServiceResult<AuthResponse>> RegisterClientAsync(
        RegisterClientRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRegistration(request);
        if (validationError is not null)
        {
            return ServiceResult<AuthResponse>.Failure(ResultStatus.Validation, validationError.Code, validationError.Message);
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        if (normalizedEmail == DefaultAdminEmail
            || await specialists.EmailExistsAsync(normalizedEmail, cancellationToken)
            || await clients.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return ServiceResult<AuthResponse>.Failure(
                ResultStatus.Conflict,
                "email_conflict",
                "An account with this email already exists.");
        }

        var phone = request.Phone.Trim();
        var existingClient = await clients.GetByPhoneAsync(phone, cancellationToken);
        if (existingClient is not null && !string.IsNullOrWhiteSpace(existingClient.Email))
        {
            return ServiceResult<AuthResponse>.Failure(
                ResultStatus.Conflict,
                "phone_conflict",
                "A client with this phone already exists.");
        }

        var passwordHash = passwordHasher.Hash(request.Password);
        var client = existingClient ?? new Client(request.FullName, phone);
        client.Rename(request.FullName);
        client.ChangePhone(phone);
        client.SetCredentials(normalizedEmail, passwordHash);

        if (existingClient is null)
        {
            await clients.AddAsync(client, cancellationToken);
        }

        return await IssueTokensAsync(client, cancellationToken);
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (!IsValidEmail(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return InvalidCredentials();
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        if (normalizedEmail == DefaultAdminEmail)
        {
            var admin = await admins.GetAdminByEmailAsync(normalizedEmail, cancellationToken);
            if (admin is not null && admin.IsActive && passwordHasher.Verify(request.Password, admin.PasswordHash))
            {
                return await IssueTokensAsync(admin, cancellationToken);
            }

            return InvalidCredentials();
        }

        var specialist = await specialists.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (specialist is null || !passwordHasher.Verify(request.Password, specialist.PasswordHash))
        {
            var client = await clients.GetByEmailAsync(normalizedEmail, cancellationToken);
            if (client is null
                || string.IsNullOrWhiteSpace(client.PasswordHash)
                || !passwordHasher.Verify(request.Password, client.PasswordHash))
            {
                return InvalidCredentials();
            }

            return await IssueTokensAsync(client, cancellationToken);
        }

        if (specialist.IsBlocked)
        {
            return ServiceResult<AuthResponse>.Failure(
                ResultStatus.Unauthorized,
                "specialist_blocked",
                "Specialist account is blocked.");
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
            refreshToken.ExpiresAt)
        {
            Role = "Specialist"
        });
    }

    private async Task<ServiceResult<AuthResponse>> IssueTokensAsync(
        Client client,
        CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;
        var accessToken = tokenService.CreateClientAccessToken(client, utcNow);
        var refreshToken = tokenService.CreateRefreshToken(utcNow);

        client.SetRefreshToken(passwordHasher.Hash(refreshToken.Token), refreshToken.ExpiresAt);
        await clients.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthResponse>.Success(new AuthResponse(
            Guid.Empty,
            client.FullName,
            client.Email ?? string.Empty,
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt)
        {
            Role = "Client",
            ClientId = client.Id
        });
    }

    private async Task<ServiceResult<AuthResponse>> IssueTokensAsync(
        AdminUser admin,
        CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;
        var accessToken = tokenService.CreateAdminAccessToken(admin, utcNow);

        admin.RecordLogin(utcNow);
        await admins.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthResponse>.Success(new AuthResponse(
            Guid.Empty,
            admin.FullName,
            admin.Email,
            accessToken.Token,
            accessToken.ExpiresAt,
            string.Empty,
            accessToken.ExpiresAt)
        {
            Role = "Admin",
            AdminId = admin.Id
        });
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
        return ValidateRegistration(
            request.FullName,
            request.Email,
            request.Phone,
            request.Password,
            request.ConfirmPassword);
    }

    private static ServiceError? ValidateRegistration(RegisterClientRequest request)
    {
        return ValidateRegistration(
            request.FullName,
            request.Email,
            request.Phone,
            request.Password,
            request.ConfirmPassword);
    }

    private static ServiceError? ValidateRegistration(
        string fullName,
        string email,
        string phone,
        string password,
        string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length < 2)
        {
            return new ServiceError("invalid_full_name", "Full name must contain at least 2 characters.");
        }

        if (!IsValidEmail(email))
        {
            return new ServiceError("invalid_email", "Email format is invalid.");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            return new ServiceError("invalid_phone", "Phone is required.");
        }

        if (password != confirmPassword)
        {
            return new ServiceError("password_mismatch", "Password confirmation does not match.");
        }

        if (!IsStrongPassword(password))
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

    private async Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return normalizedEmail == DefaultAdminEmail
            || await admins.AdminEmailExistsAsync(normalizedEmail, cancellationToken)
            || await specialists.EmailExistsAsync(normalizedEmail, cancellationToken)
            || await clients.EmailExistsAsync(normalizedEmail, cancellationToken);
    }
}
