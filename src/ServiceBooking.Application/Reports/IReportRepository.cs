using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Reports;

public interface IReportRepository
{
    Task<IReadOnlyCollection<Booking>> ListCompletedAsync(
        Guid specialistId,
        CancellationToken cancellationToken);
}
