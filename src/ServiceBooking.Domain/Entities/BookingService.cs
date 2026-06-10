namespace ServiceBooking.Domain.Entities;

public sealed class BookingService
{
    private BookingService()
    {
        ServiceName = string.Empty;
    }

    public BookingService(Guid serviceId, string serviceName, decimal price, int durationMinutes)
    {
        if (serviceId == Guid.Empty)
        {
            throw new ArgumentException("Service id is required.", nameof(serviceId));
        }

        Id = Guid.NewGuid();
        ServiceId = serviceId;
        ServiceName = ValidateRequired(serviceName, nameof(serviceName), 120);
        SetPrice(price);
        SetDuration(durationMinutes);
    }

    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Booking? Booking { get; private set; }
    public Guid ServiceId { get; private set; }
    public string ServiceName { get; private set; }
    public decimal Price { get; private set; }
    public int DurationMinutes { get; private set; }

    private void SetPrice(decimal price)
    {
        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        }

        Price = decimal.Round(price, 2);
    }

    private void SetDuration(int durationMinutes)
    {
        if (durationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Duration must be greater than zero.");
        }

        DurationMinutes = durationMinutes;
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
