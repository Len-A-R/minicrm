namespace ServiceBooking.Domain.Entities;

public sealed class AdminUser
{
    private AdminUser()
    {
        FullName = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    public AdminUser(string fullName, string email, string passwordHash)
    {
        Id = Guid.NewGuid();
        FullName = ValidateRequired(fullName, nameof(fullName), 100, 2);
        Email = ValidateRequired(email, nameof(email), 254, 3).ToLowerInvariant();
        PasswordHash = ValidateRequired(passwordHash, nameof(passwordHash), 500, 8);
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    public void Update(string fullName, string email, bool isActive)
    {
        FullName = ValidateRequired(fullName, nameof(fullName), 100, 2);
        Email = ValidateRequired(email, nameof(email), 254, 3).ToLowerInvariant();
        IsActive = isActive;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        PasswordHash = ValidateRequired(passwordHash, nameof(passwordHash), 500, 8);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void RecordLogin(DateTimeOffset utcNow) => LastLoginAt = utcNow;

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
