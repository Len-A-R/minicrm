using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Admin;

public interface ISubscriptionQuotaService
{
    Task<ServiceResult<bool>> CheckBookingQuotaAsync(Guid specialistId, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> CheckServiceQuotaAsync(Guid specialistId, CancellationToken cancellationToken);
}
