namespace ServiceBooking.Domain.Entities;

public sealed class Vacation
{
    private Vacation()
    {
    }

    public Vacation(Guid specialistId, DateOnly date, string? reason = null)
    {
        if (specialistId == Guid.Empty)
        {
            throw new ArgumentException("Specialist id is required.", nameof(specialistId));
        }

        Id = Guid.NewGuid();
        SpecialistId = specialistId;
        Date = date;
        SetReason(reason);
    }

    public Guid Id { get; private set; }
    public Guid SpecialistId { get; private set; }
    public Specialist? Specialist { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Reason { get; private set; }

    public void SetReason(string? reason)
    {
        if (reason is { Length: > 250 })
        {
            throw new ArgumentException("Vacation reason cannot exceed 250 characters.", nameof(reason));
        }

        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }
}
