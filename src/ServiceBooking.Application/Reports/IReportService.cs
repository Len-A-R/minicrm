using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Reports;

public interface IReportService
{
    Task<ServiceResult<ReportSummaryResponse>> GetSummaryAsync(
        Guid specialistId,
        ReportPeriodQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<RevenueByServiceResponse>>> GetByServiceAsync(
        Guid specialistId,
        ReportPeriodQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<RevenueByClientResponse>>> GetByClientAsync(
        Guid specialistId,
        ReportPeriodQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<RevenueByDayResponse>>> GetByDayAsync(
        Guid specialistId,
        ReportPeriodQuery query,
        CancellationToken cancellationToken);
}
