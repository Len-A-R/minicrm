using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Domain.Entities;

public sealed class PaymentTransaction
{
    private PaymentTransaction()
    {
        Currency = string.Empty;
        Provider = string.Empty;
    }

    public PaymentTransaction(Guid specialistId, Guid? subscriptionId, decimal amount, string currency, string provider)
    {
        if (specialistId == Guid.Empty)
        {
            throw new ArgumentException("Specialist id is required.", nameof(specialistId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");
        }

        Id = Guid.NewGuid();
        SpecialistId = specialistId;
        SubscriptionId = subscriptionId;
        Amount = decimal.Round(amount, 2);
        Currency = ValidateRequired(currency, nameof(currency), 8).ToUpperInvariant();
        Provider = ValidateRequired(provider, nameof(provider), 80);
        Status = PaymentStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid SpecialistId { get; private set; }
    public Specialist? Specialist { get; private set; }
    public Guid? SubscriptionId { get; private set; }
    public SpecialistSubscription? Subscription { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public string Provider { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? ExternalId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }

    public void MarkSucceeded(string? externalId, DateTimeOffset paidAt)
    {
        Status = PaymentStatus.Succeeded;
        ExternalId = string.IsNullOrWhiteSpace(externalId) ? ExternalId : externalId.Trim();
        PaidAt = paidAt;
        FailureReason = null;
    }

    public void MarkFailed(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Failure reason is required.", nameof(reason));
        }

        Status = PaymentStatus.Failed;
        FailureReason = reason.Trim();
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
