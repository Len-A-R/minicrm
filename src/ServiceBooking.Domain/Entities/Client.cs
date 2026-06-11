using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Domain.Entities;

public sealed class Client
{
    private readonly List<Booking> _bookings = [];

    private Client()
    {
        FullName = string.Empty;
        Phone = string.Empty;
    }

    public Client(string fullName, string phone, string? email = null, string? passwordHash = null)
    {
        Id = Guid.NewGuid();
        FullName = ValidateName(fullName);
        Phone = ValidateRequired(phone, nameof(phone), 32);
        Email = NormalizeOptionalEmail(email);
        PasswordHash = string.IsNullOrWhiteSpace(passwordHash)
            ? null
            : ValidateRequired(passwordHash, nameof(passwordHash), 500);
        Status = ClientStatus.Regular;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; }
    public string Phone { get; private set; }
    public string? Email { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? RefreshTokenHash { get; private set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; private set; }
    public ClientStatus Status { get; private set; }
    public string? Tag { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<Booking> Bookings => _bookings;

    public void Rename(string fullName) => FullName = ValidateName(fullName);

    public void ChangePhone(string phone) => Phone = ValidateRequired(phone, nameof(phone), 32);

    public void SetCredentials(string email, string passwordHash)
    {
        Email = NormalizeOptionalEmail(email)
            ?? throw new ArgumentException("Email is required.", nameof(email));
        PasswordHash = ValidateRequired(passwordHash, nameof(passwordHash), 500);
    }

    public void SetRefreshToken(string refreshTokenHash, DateTimeOffset expiresAt)
    {
        RefreshTokenHash = ValidateRequired(refreshTokenHash, nameof(refreshTokenHash), 500);
        RefreshTokenExpiresAt = expiresAt;
    }

    public void ClearRefreshToken()
    {
        RefreshTokenHash = null;
        RefreshTokenExpiresAt = null;
    }

    public bool HasActiveRefreshToken(DateTimeOffset utcNow)
    {
        return !string.IsNullOrWhiteSpace(RefreshTokenHash)
            && RefreshTokenExpiresAt.HasValue
            && RefreshTokenExpiresAt.Value > utcNow;
    }

    public void ChangeStatus(ClientStatus status) => Status = status;

    public void SetTag(string? tag)
    {
        if (tag is { Length: > 200 })
        {
            throw new ArgumentException("Client tag cannot exceed 200 characters.", nameof(tag));
        }

        Tag = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim();
    }

    private static string ValidateName(string value)
    {
        var name = ValidateRequired(value, nameof(value), 100);
        if (name.Length < 2)
        {
            throw new ArgumentException("Client name must contain at least 2 characters.", nameof(value));
        }

        return name;
    }

    private static string ValidateRequired(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        }

        return trimmed;
    }

    private static string? NormalizeOptionalEmail(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : ValidateRequired(value, nameof(value), 254).ToLowerInvariant();
    }
}
