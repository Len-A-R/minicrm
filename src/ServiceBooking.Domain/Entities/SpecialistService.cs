namespace ServiceBooking.Domain.Entities;

public sealed class SpecialistService
{
    private SpecialistService()
    {
    }

    public SpecialistService(Guid specialistId, Guid serviceId, decimal price, int durationMinutes)
    {
        if (specialistId == Guid.Empty)
        {
            throw new ArgumentException("Specialist id is required.", nameof(specialistId));
        }

        if (serviceId == Guid.Empty)
        {
            throw new ArgumentException("Service id is required.", nameof(serviceId));
        }

        Id = Guid.NewGuid();
        SpecialistId = specialistId;
        ServiceId = serviceId;
        SetPrice(price);
        SetDuration(durationMinutes);
    }

    public Guid Id { get; private set; }
    public Guid SpecialistId { get; private set; }
    public Specialist? Specialist { get; private set; }
    public Guid ServiceId { get; private set; }
    public Service? Service { get; private set; }
    public decimal Price { get; private set; }
    public int DurationMinutes { get; private set; }

    public void SetPrice(decimal price)
    {
        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        }

        Price = decimal.Round(price, 2);
    }

    public void SetDuration(int durationMinutes)
    {
        if (durationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Duration must be greater than zero.");
        }

        DurationMinutes = durationMinutes;
    }
}
