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

    public Client(string fullName, string phone)
    {
        Id = Guid.NewGuid();
        FullName = ValidateName(fullName);
        Phone = ValidateRequired(phone, nameof(phone), 32);
        Status = ClientStatus.Regular;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; }
    public string Phone { get; private set; }
    public ClientStatus Status { get; private set; }
    public string? Tag { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<Booking> Bookings => _bookings;

    public void Rename(string fullName) => FullName = ValidateName(fullName);

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
}
