namespace ServiceBooking.Domain.Entities;

public sealed class Specialist
{
    private readonly List<Booking> _bookings = [];
    private readonly List<SpecialistService> _services = [];
    private readonly List<Vacation> _vacations = [];

    private Specialist()
    {
        FullName = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
        PasswordHash = string.Empty;
    }

    public Specialist(string fullName, string email, string phone, string passwordHash)
    {
        Id = Guid.NewGuid();
        FullName = ValidateRequired(fullName, nameof(fullName), 100, 2);
        Email = ValidateRequired(email, nameof(email), 254, 3);
        Phone = ValidateRequired(phone, nameof(phone), 32, 3);
        PasswordHash = ValidateRequired(passwordHash, nameof(passwordHash), 500, 8);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string Phone { get; private set; }
    public string PasswordHash { get; private set; }
    public string? RefreshTokenHash { get; private set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? VenueName { get; private set; }
    public Guid? LocationId { get; private set; }
    public Location? Location { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<SpecialistService> Services => _services;
    public IReadOnlyCollection<Vacation> Vacations => _vacations;
    public IReadOnlyCollection<Booking> Bookings => _bookings;

    public void UpdateProfile(string fullName, string phone, string? venueName, Guid? locationId)
    {
        FullName = ValidateRequired(fullName, nameof(fullName), 100, 2);
        Phone = ValidateRequired(phone, nameof(phone), 32, 3);
        VenueName = string.IsNullOrWhiteSpace(venueName) ? null : ValidateRequired(venueName, nameof(venueName), 160, 2);
        LocationId = locationId;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        PasswordHash = ValidateRequired(passwordHash, nameof(passwordHash), 500, 8);
    }

    public void SetRefreshToken(string refreshTokenHash, DateTimeOffset expiresAt)
    {
        RefreshTokenHash = ValidateRequired(refreshTokenHash, nameof(refreshTokenHash), 500, 8);
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

    public void SetAvatarUrl(string? avatarUrl)
    {
        if (avatarUrl is { Length: > 500 })
        {
            throw new ArgumentException("Avatar URL cannot exceed 500 characters.", nameof(avatarUrl));
        }

        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
    }

    private static string ValidateRequired(string value, string parameterName, int maxLength, int minLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length < minLength)
        {
            throw new ArgumentException($"Value must contain at least {minLength} characters.", parameterName);
        }

        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        }

        return trimmed;
    }
}
