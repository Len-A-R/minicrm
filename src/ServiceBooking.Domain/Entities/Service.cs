namespace ServiceBooking.Domain.Entities;

public sealed class Service
{
    private readonly List<SpecialistService> _specialistServices = [];

    private Service()
    {
        Name = string.Empty;
    }

    public Service(string name, string? description = null)
    {
        Id = Guid.NewGuid();
        Name = ValidateRequired(name, nameof(name), 120);
        SetDescription(description);
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public IReadOnlyCollection<SpecialistService> SpecialistServices => _specialistServices;

    public void Rename(string name) => Name = ValidateRequired(name, nameof(name), 120);

    public void SetDescription(string? description)
    {
        if (description is { Length: > 500 })
        {
            throw new ArgumentException("Service description cannot exceed 500 characters.", nameof(description));
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
