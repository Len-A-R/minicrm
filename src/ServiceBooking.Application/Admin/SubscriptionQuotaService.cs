using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Admin;

public sealed class SubscriptionQuotaService(
    IAdminRepository repository,
    IDateTimeProvider dateTimeProvider) : ISubscriptionQuotaService
{
    public async Task<ServiceResult<bool>> CheckBookingQuotaAsync(Guid specialistId, CancellationToken cancellationToken)
    {
        var subscription = await repository.GetActiveSubscriptionAsync(specialistId, cancellationToken);
        if (subscription?.Plan is null || !subscription.IsUsable(dateTimeProvider.UtcNow) || subscription.Plan.BookingLimit == 0)
        {
            return ServiceResult<bool>.Success(true);
        }

        var now = dateTimeProvider.UtcNow;
        var from = new DateOnly(now.Year, now.Month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var count = await repository.CountBookingsAsync(specialistId, from, to, cancellationToken);
        if (count >= subscription.Plan.BookingLimit)
        {
            return ServiceResult<bool>.Failure(
                ResultStatus.Conflict,
                "booking_quota_exceeded",
                "Subscription booking quota is exceeded.");
        }

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> CheckServiceQuotaAsync(Guid specialistId, CancellationToken cancellationToken)
    {
        var subscription = await repository.GetActiveSubscriptionAsync(specialistId, cancellationToken);
        if (subscription?.Plan is null || !subscription.IsUsable(dateTimeProvider.UtcNow) || subscription.Plan.ServiceLimit == 0)
        {
            return ServiceResult<bool>.Success(true);
        }

        var count = await repository.CountSpecialistServicesAsync(specialistId, cancellationToken);
        if (count >= subscription.Plan.ServiceLimit)
        {
            return ServiceResult<bool>.Failure(
                ResultStatus.Conflict,
                "service_quota_exceeded",
                "Subscription service quota is exceeded.");
        }

        return ServiceResult<bool>.Success(true);
    }
}
