namespace ServiceBooking.Domain.Entities;

public sealed class SystemSetting
{
    private SystemSetting()
    {
        Key = string.Empty;
        Value = string.Empty;
    }

    public SystemSetting(string key, string value, string? description = null)
    {
        Id = Guid.NewGuid();
        Key = ValidateRequired(key, nameof(key), 120);
        Value = string.Empty;
        SetValue(value);
        SetDescription(description);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Key { get; private set; }
    public string Value { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(string value, string? description, DateTimeOffset utcNow)
    {
        SetValue(value);
        SetDescription(description);
        UpdatedAt = utcNow;
    }

    private void SetValue(string value)
    {
        Value = ValidateRequired(value, nameof(value), 2000);
    }

    private void SetDescription(string? description)
    {
        if (description is { Length: > 500 })
        {
            throw new ArgumentException("Setting description cannot exceed 500 characters.", nameof(description));
        }

        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
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
}
