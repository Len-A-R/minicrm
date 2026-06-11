namespace ServiceBooking.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog()
    {
        ActorType = string.Empty;
        Action = string.Empty;
        EntityType = string.Empty;
        Outcome = string.Empty;
    }

    public AuditLog(
        Guid? actorId,
        string actorType,
        string action,
        string entityType,
        string? entityId,
        string outcome,
        string? details,
        string? ipAddress,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        ActorId = actorId;
        ActorType = ValidateRequired(actorType, nameof(actorType), 40);
        Action = ValidateRequired(action, nameof(action), 120);
        EntityType = ValidateRequired(entityType, nameof(entityType), 120);
        EntityId = string.IsNullOrWhiteSpace(entityId) ? null : entityId.Trim();
        Outcome = ValidateRequired(outcome, nameof(outcome), 40);
        Details = details is { Length: > 2000 } ? details[..2000] : details;
        IpAddress = ipAddress is { Length: > 80 } ? ipAddress[..80] : ipAddress;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid? ActorId { get; private set; }
    public string ActorType { get; private set; }
    public string Action { get; private set; }
    public string EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public string Outcome { get; private set; }
    public string? Details { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

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
