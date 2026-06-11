using ServiceBooking.Application.Common;
using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Reports;

public sealed class ReportService(IReportRepository reports) : IReportService
{
    public async Task<ServiceResult<ReportSummaryResponse>> GetSummaryAsync(
        Guid specialistId,
        ReportPeriodQuery query,
        CancellationToken cancellationToken)
    {
        var bookingsResult = await GetFilteredBookingsAsync(specialistId, query, cancellationToken);
        if (!bookingsResult.IsSuccess)
        {
            return ServiceResult<ReportSummaryResponse>.Failure(
                bookingsResult.Status,
                bookingsResult.Error!.Code,
                bookingsResult.Error.Message);
        }

        var bookings = bookingsResult.Value!;
        var totalRevenue = bookings.Sum(Revenue);
        var completedBookings = bookings.Count;
        var averageCheck = completedBookings == 0 ? 0 : decimal.Round(totalRevenue / completedBookings, 2);

        return ServiceResult<ReportSummaryResponse>.Success(new ReportSummaryResponse(
            query.From,
            query.To,
            totalRevenue,
            completedBookings,
            averageCheck));
    }

    public async Task<ServiceResult<IReadOnlyCollection<RevenueByServiceResponse>>> GetByServiceAsync(
        Guid specialistId,
        ReportPeriodQuery query,
        CancellationToken cancellationToken)
    {
        var bookingsResult = await GetFilteredBookingsAsync(specialistId, query, cancellationToken);
        if (!bookingsResult.IsSuccess)
        {
            return Failure<IReadOnlyCollection<RevenueByServiceResponse>>(bookingsResult);
        }

        var items = bookingsResult.Value!
            .SelectMany(ToServiceRevenueItems)
            .GroupBy(item => new { item.ServiceId, item.ServiceName })
            .Select(group => new RevenueByServiceResponse(
                group.Key.ServiceId,
                group.Key.ServiceName,
                group.Sum(item => item.Revenue),
                group.Select(item => item.BookingId).Distinct().Count(),
                group.Count()))
            .OrderByDescending(item => item.Revenue)
            .ThenBy(item => item.ServiceName)
            .ToArray();

        return ServiceResult<IReadOnlyCollection<RevenueByServiceResponse>>.Success(items);
    }

    public async Task<ServiceResult<IReadOnlyCollection<RevenueByClientResponse>>> GetByClientAsync(
        Guid specialistId,
        ReportPeriodQuery query,
        CancellationToken cancellationToken)
    {
        var bookingsResult = await GetFilteredBookingsAsync(specialistId, query, cancellationToken);
        if (!bookingsResult.IsSuccess)
        {
            return Failure<IReadOnlyCollection<RevenueByClientResponse>>(bookingsResult);
        }

        var items = bookingsResult.Value!
            .GroupBy(booking => new
            {
                booking.ClientId,
                booking.ClientName,
                booking.ClientPhone
            })
            .Select(group => new RevenueByClientResponse(
                group.Key.ClientId,
                group.Key.ClientName,
                group.Key.ClientPhone,
                group.Sum(Revenue),
                group.Count()))
            .OrderByDescending(item => item.Revenue)
            .ThenBy(item => item.ClientName)
            .ToArray();

        return ServiceResult<IReadOnlyCollection<RevenueByClientResponse>>.Success(items);
    }

    public async Task<ServiceResult<IReadOnlyCollection<RevenueByDayResponse>>> GetByDayAsync(
        Guid specialistId,
        ReportPeriodQuery query,
        CancellationToken cancellationToken)
    {
        var bookingsResult = await GetFilteredBookingsAsync(specialistId, query, cancellationToken);
        if (!bookingsResult.IsSuccess)
        {
            return Failure<IReadOnlyCollection<RevenueByDayResponse>>(bookingsResult);
        }

        var byDay = bookingsResult.Value!
            .GroupBy(CompletedDate)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var revenue = group.Sum(Revenue);
                    var count = group.Count();
                    return new RevenueByDayResponse(
                        group.Key,
                        revenue,
                        count,
                        count == 0 ? 0 : decimal.Round(revenue / count, 2));
                });

        var items = EachDay(query.From, query.To)
            .Select(date => byDay.TryGetValue(date, out var item)
                ? item
                : new RevenueByDayResponse(date, 0, 0, 0))
            .ToArray();

        return ServiceResult<IReadOnlyCollection<RevenueByDayResponse>>.Success(items);
    }

    private async Task<ServiceResult<IReadOnlyCollection<Booking>>> GetFilteredBookingsAsync(
        Guid specialistId,
        ReportPeriodQuery query,
        CancellationToken cancellationToken)
    {
        if (specialistId == Guid.Empty)
        {
            return Validation<IReadOnlyCollection<Booking>>("invalid_specialist_id", "Specialist id is required.");
        }

        if (query.From == default || query.To == default || query.From > query.To)
        {
            return Validation<IReadOnlyCollection<Booking>>("invalid_period", "Report period is invalid.");
        }

        var bookings = await reports.ListCompletedAsync(specialistId, cancellationToken);
        return ServiceResult<IReadOnlyCollection<Booking>>.Success(bookings
            .Where(booking => booking.CompletedAt.HasValue)
            .Where(booking =>
            {
                var date = CompletedDate(booking);
                return date >= query.From && date <= query.To;
            })
            .ToArray());
    }

    private static IEnumerable<ServiceRevenueItem> ToServiceRevenueItems(Booking booking)
    {
        var services = booking.Services.ToArray();
        if (services.Length == 0)
        {
            yield return new ServiceRevenueItem(booking.Id, Guid.Empty, "Без услуги", Revenue(booking));
            yield break;
        }

        var servicesTotal = services.Sum(service => service.Price);
        var bookingRevenue = Revenue(booking);
        var allocatedRevenue = 0m;
        for (var index = 0; index < services.Length; index++)
        {
            var service = services[index];
            var share = index == services.Length - 1
                ? bookingRevenue - allocatedRevenue
                : servicesTotal <= 0
                    ? bookingRevenue / services.Length
                    : bookingRevenue * service.Price / servicesTotal;
            share = decimal.Round(share, 2);
            allocatedRevenue += share;
            yield return new ServiceRevenueItem(
                booking.Id,
                service.ServiceId,
                service.ServiceName,
                share);
        }
    }

    private static decimal Revenue(Booking booking) => decimal.Round(booking.ActualRevenue ?? booking.TotalPrice, 2);

    private static DateOnly CompletedDate(Booking booking)
    {
        return DateOnly.FromDateTime(booking.CompletedAt!.Value.DateTime);
    }

    private static IEnumerable<DateOnly> EachDay(DateOnly from, DateOnly to)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    private static ServiceResult<T> Validation<T>(string code, string message)
    {
        return ServiceResult<T>.Failure(ResultStatus.Validation, code, message);
    }

    private static ServiceResult<T> Failure<T>(ServiceResult<IReadOnlyCollection<Booking>> result)
    {
        return ServiceResult<T>.Failure(result.Status, result.Error!.Code, result.Error.Message);
    }

    private sealed record ServiceRevenueItem(Guid BookingId, Guid ServiceId, string ServiceName, decimal Revenue);
}
