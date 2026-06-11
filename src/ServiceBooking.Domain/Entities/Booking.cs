using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Domain.Entities;

public sealed class Booking
{
    private readonly List<BookingService> _services = [];

    private Booking()
    {
        ClientName = string.Empty;
        ClientPhone = string.Empty;
    }

    public Booking(
        string clientName,
        string clientPhone,
        Guid specialistId,
        DateOnly requestedDate,
        TimeOnly requestedTime,
        IEnumerable<BookingService> services,
        string? message = null,
        Guid? clientId = null)
    {
        if (specialistId == Guid.Empty)
        {
            throw new ArgumentException("Specialist id is required.", nameof(specialistId));
        }

        Id = Guid.NewGuid();
        ClientName = ValidateRequired(clientName, nameof(clientName), 100, 2);
        ClientPhone = ValidateRequired(clientPhone, nameof(clientPhone), 32, 3);
        SpecialistId = specialistId;
        ClientId = clientId;
        RequestedDate = requestedDate;
        RequestedTime = requestedTime;
        Status = BookingStatus.New;
        CreatedAt = DateTimeOffset.UtcNow;
        SetMessage(message);

        foreach (var service in services)
        {
            _services.Add(service);
        }

        RecalculateTotals();
    }

    public Guid Id { get; private set; }
    public string ClientName { get; private set; }
    public string ClientPhone { get; private set; }
    public Guid SpecialistId { get; private set; }
    public Specialist? Specialist { get; private set; }
    public Guid? ClientId { get; private set; }
    public Client? Client { get; private set; }
    public IReadOnlyCollection<BookingService> Services => _services;
    public DateOnly RequestedDate { get; private set; }
    public TimeOnly RequestedTime { get; private set; }
    public string? Message { get; private set; }
    public decimal TotalPrice { get; private set; }
    public int TotalDuration { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateOnly? ConfirmedDate { get; private set; }
    public TimeOnly? ConfirmedTime { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public decimal? ActualRevenue { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? SpecialistReply { get; private set; }
    public DateTimeOffset? RepliedAt { get; private set; }

    public void SetMessage(string? message)
    {
        if (message is { Length: > 500 })
        {
            throw new ArgumentException("Message cannot exceed 500 characters.", nameof(message));
        }

        Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
    }

    public void Confirm(DateOnly date, TimeOnly time)
    {
        Status = BookingStatus.Confirmed;
        ConfirmedDate = date;
        ConfirmedTime = time;
        ConfirmedAt = DateTimeOffset.UtcNow;
        CompletedAt = null;
        ActualRevenue = null;
        RejectionReason = null;
    }

    public void Reject() => Status = BookingStatus.Rejected;

    public void Reject(string? reason)
    {
        if (reason is { Length: > 500 })
        {
            throw new ArgumentException("Rejection reason cannot exceed 500 characters.", nameof(reason));
        }

        Status = BookingStatus.Rejected;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public void Reply(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new ArgumentException("Reply is required.", nameof(reply));
        }

        var trimmed = reply.Trim();
        if (trimmed.Length > 1000)
        {
            throw new ArgumentException("Reply cannot exceed 1000 characters.", nameof(reply));
        }

        SpecialistReply = trimmed;
        RepliedAt = DateTimeOffset.UtcNow;
    }

    public void Reopen()
    {
        Status = BookingStatus.New;
        ConfirmedAt = null;
        ConfirmedDate = null;
        ConfirmedTime = null;
        CompletedAt = null;
        ActualRevenue = null;
        RejectionReason = null;
    }

    public void Complete(decimal actualRevenue)
    {
        if (actualRevenue < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(actualRevenue), "Actual revenue cannot be negative.");
        }

        Status = BookingStatus.Completed;
        ActualRevenue = decimal.Round(actualRevenue, 2);
        CompletedAt = DateTimeOffset.UtcNow;
    }

    private void RecalculateTotals()
    {
        TotalPrice = _services.Sum(service => service.Price);
        TotalDuration = _services.Sum(service => service.DurationMinutes);
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
