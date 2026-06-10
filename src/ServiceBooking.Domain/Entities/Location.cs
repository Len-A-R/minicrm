namespace ServiceBooking.Domain.Entities;

public sealed class Location
{
    private readonly List<Specialist> _specialists = [];

    private Location()
    {
        Name = string.Empty;
        Address = string.Empty;
    }

    public Location(string name, string address, string? description = null)
    {
        Id = Guid.NewGuid();
        Name = ValidateRequired(name, nameof(name), 120);
        Address = ValidateRequired(address, nameof(address), 250);
        SetDescription(description);
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Address { get; private set; }
    public string? Description { get; private set; }
    public IReadOnlyCollection<Specialist> Specialists => _specialists;

    public void Rename(string name) => Name = ValidateRequired(name, nameof(name), 120);

    public void ChangeAddress(string address) => Address = ValidateRequired(address, nameof(address), 250);

    public void SetDescription(string? description)
    {
        if (description is { Length: > 500 })
        {
            throw new ArgumentException("Location description cannot exceed 500 characters.", nameof(description));
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
