namespace ServiceBooking.Domain.Entities;

public sealed class SubscriptionPlan
{
    private readonly List<SpecialistSubscription> _subscriptions = [];

    private SubscriptionPlan()
    {
        Name = string.Empty;
    }

    public SubscriptionPlan(
        string name,
        decimal monthlyPrice,
        int bookingLimit,
        int serviceLimit,
        string? description = null)
    {
        Id = Guid.NewGuid();
        Name = ValidateRequired(name, nameof(name), 120);
        SetDescription(description);
        SetLimits(bookingLimit, serviceLimit);
        SetMonthlyPrice(monthlyPrice);
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public decimal MonthlyPrice { get; private set; }
    public int BookingLimit { get; private set; }
    public int ServiceLimit { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<SpecialistSubscription> Subscriptions => _subscriptions;

    public void Update(string name, string? description, decimal monthlyPrice, int bookingLimit, int serviceLimit, bool isActive)
    {
        Name = ValidateRequired(name, nameof(name), 120);
        SetDescription(description);
        SetMonthlyPrice(monthlyPrice);
        SetLimits(bookingLimit, serviceLimit);
        IsActive = isActive;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private void SetDescription(string? description)
    {
        if (description is { Length: > 500 })
        {
            throw new ArgumentException("Plan description cannot exceed 500 characters.", nameof(description));
        }

        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private void SetMonthlyPrice(decimal monthlyPrice)
    {
        if (monthlyPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyPrice), "Monthly price cannot be negative.");
        }

        MonthlyPrice = decimal.Round(monthlyPrice, 2);
    }

    private void SetLimits(int bookingLimit, int serviceLimit)
    {
        if (bookingLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bookingLimit), "Booking limit cannot be negative.");
        }

        if (serviceLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(serviceLimit), "Service limit cannot be negative.");
        }

        BookingLimit = bookingLimit;
        ServiceLimit = serviceLimit;
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
