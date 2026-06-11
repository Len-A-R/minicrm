using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Domain.Entities;

public sealed class SpecialistSubscription
{
    private SpecialistSubscription()
    {
    }

    public SpecialistSubscription(Guid specialistId, Guid planId, DateTimeOffset startedAt, DateTimeOffset expiresAt)
    {
        if (specialistId == Guid.Empty)
        {
            throw new ArgumentException("Specialist id is required.", nameof(specialistId));
        }

        if (planId == Guid.Empty)
        {
            throw new ArgumentException("Plan id is required.", nameof(planId));
        }

        if (expiresAt <= startedAt)
        {
            throw new ArgumentException("Subscription expiration must be after start date.", nameof(expiresAt));
        }

        Id = Guid.NewGuid();
        SpecialistId = specialistId;
        PlanId = planId;
        Status = SubscriptionStatus.Active;
        StartedAt = startedAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }
    public Guid SpecialistId { get; private set; }
    public Specialist? Specialist { get; private set; }
    public Guid PlanId { get; private set; }
    public SubscriptionPlan? Plan { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RenewedAt { get; private set; }

    public bool IsUsable(DateTimeOffset utcNow)
    {
        return (Status == SubscriptionStatus.Active || Status == SubscriptionStatus.Trial) && ExpiresAt > utcNow;
    }

    public void ChangePlan(Guid planId)
    {
        if (planId == Guid.Empty)
        {
            throw new ArgumentException("Plan id is required.", nameof(planId));
        }

        PlanId = planId;
    }

    public void ChangeStatus(SubscriptionStatus status) => Status = status;

    public void Renew(DateTimeOffset expiresAt, DateTimeOffset utcNow)
    {
        if (expiresAt <= utcNow)
        {
            throw new ArgumentException("Subscription expiration must be in the future.", nameof(expiresAt));
        }

        ExpiresAt = expiresAt;
        RenewedAt = utcNow;
        Status = SubscriptionStatus.Active;
    }
}
