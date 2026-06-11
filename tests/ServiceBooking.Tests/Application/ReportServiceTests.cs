using ServiceBooking.Application.Common;
using ServiceBooking.Application.Reports;
using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Tests.Application;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task GetSummaryAndByDayAsync_ReturnCompletedRevenueForPeriod()
    {
        var specialistId = Guid.NewGuid();
        var reportService = new ReportService(new FakeReportRepository
        {
            Bookings =
            {
                Complete(CreateBooking(specialistId, "Alice Brown", "+15550101010", 100m), 120m, new DateOnly(2026, 7, 10)),
                Complete(CreateBooking(specialistId, "Bob Stone", "+15550202020", 200m), 180m, new DateOnly(2026, 7, 11)),
                Complete(CreateBooking(specialistId, "Outside Period", "+15550303030", 50m), 60m, new DateOnly(2026, 6, 20)),
                CreateBooking(specialistId, "Not Completed", "+15550404040", 90m)
            }
        });

        var summary = await reportService.GetSummaryAsync(
            specialistId,
            new ReportPeriodQuery(new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 12)),
            CancellationToken.None);
        var byDay = await reportService.GetByDayAsync(
            specialistId,
            new ReportPeriodQuery(new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 12)),
            CancellationToken.None);

        Assert.True(summary.IsSuccess);
        Assert.Equal(300m, summary.Value!.TotalRevenue);
        Assert.Equal(2, summary.Value.CompletedBookings);
        Assert.Equal(150m, summary.Value.AverageCheck);
        Assert.True(byDay.IsSuccess);
        Assert.Equal(3, byDay.Value!.Count);
        Assert.Equal(120m, byDay.Value.Single(item => item.Date == new DateOnly(2026, 7, 10)).Revenue);
        Assert.Equal(0m, byDay.Value.Single(item => item.Date == new DateOnly(2026, 7, 12)).Revenue);
    }

    [Fact]
    public async Task GetByServiceAsync_DistributesActualRevenueProportionally()
    {
        var specialistId = Guid.NewGuid();
        var serviceA = Guid.NewGuid();
        var serviceB = Guid.NewGuid();
        var reportService = new ReportService(new FakeReportRepository
        {
            Bookings =
            {
                Complete(
                    CreateBooking(
                        specialistId,
                        "Alice Brown",
                        "+15550101010",
                        new BookingService(serviceA, "Consultation", 100m, 30),
                        new BookingService(serviceB, "Therapy", 300m, 90)),
                    200m,
                    new DateOnly(2026, 7, 10)),
                Complete(
                    CreateBooking(specialistId, "Bob Stone", "+15550202020", new BookingService(serviceA, "Consultation", 100m, 30)),
                    90m,
                    new DateOnly(2026, 7, 11))
            }
        });

        var result = await reportService.GetByServiceAsync(
            specialistId,
            new ReportPeriodQuery(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var consultation = result.Value!.Single(item => item.ServiceId == serviceA);
        var therapy = result.Value!.Single(item => item.ServiceId == serviceB);
        Assert.Equal(140m, consultation.Revenue);
        Assert.Equal(2, consultation.CompletedBookings);
        Assert.Equal(2, consultation.Quantity);
        Assert.Equal(150m, therapy.Revenue);
        Assert.Equal(1, therapy.CompletedBookings);
    }

    [Fact]
    public async Task GetByClientAsync_GroupsByClientAndValidatesPeriod()
    {
        var specialistId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var reportService = new ReportService(new FakeReportRepository
        {
            Bookings =
            {
                Complete(CreateBooking(specialistId, "Alice Brown", "+15550101010", 100m, clientId), 120m, new DateOnly(2026, 7, 10)),
                Complete(CreateBooking(specialistId, "Alice Brown", "+15550101010", 100m, clientId), 130m, new DateOnly(2026, 7, 11)),
                Complete(CreateBooking(specialistId, "Bob Stone", "+15550202020", 100m), 90m, new DateOnly(2026, 7, 12))
            }
        });

        var byClient = await reportService.GetByClientAsync(
            specialistId,
            new ReportPeriodQuery(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31)),
            CancellationToken.None);
        var invalid = await reportService.GetSummaryAsync(
            specialistId,
            new ReportPeriodQuery(new DateOnly(2026, 8, 1), new DateOnly(2026, 7, 31)),
            CancellationToken.None);

        Assert.True(byClient.IsSuccess);
        var alice = byClient.Value!.Single(item => item.ClientId == clientId);
        Assert.Equal(250m, alice.Revenue);
        Assert.Equal(2, alice.CompletedBookings);
        Assert.Equal(ResultStatus.Validation, invalid.Status);
        Assert.Equal("invalid_period", invalid.Error?.Code);
    }

    private static Booking CreateBooking(Guid specialistId, string clientName, string clientPhone, decimal price, Guid? clientId = null)
    {
        return CreateBooking(
            specialistId,
            clientName,
            clientPhone,
            new BookingService(Guid.NewGuid(), "Consultation", price, 60),
            clientId);
    }

    private static Booking CreateBooking(
        Guid specialistId,
        string clientName,
        string clientPhone,
        BookingService service,
        Guid? clientId = null)
    {
        return CreateBooking(specialistId, clientName, clientPhone, [service], clientId);
    }

    private static Booking CreateBooking(
        Guid specialistId,
        string clientName,
        string clientPhone,
        BookingService firstService,
        BookingService secondService)
    {
        return CreateBooking(specialistId, clientName, clientPhone, [firstService, secondService]);
    }

    private static Booking CreateBooking(
        Guid specialistId,
        string clientName,
        string clientPhone,
        IEnumerable<BookingService> services,
        Guid? clientId = null)
    {
        return new Booking(
            clientName,
            clientPhone,
            specialistId,
            new DateOnly(2026, 7, 10),
            new TimeOnly(10, 0),
            services,
            clientId: clientId);
    }

    private static Booking Complete(Booking booking, decimal revenue, DateOnly date)
    {
        booking.Complete(revenue, new DateTimeOffset(date, new TimeOnly(12, 0), TimeSpan.Zero));
        return booking;
    }

    private sealed class FakeReportRepository : IReportRepository
    {
        public List<Booking> Bookings { get; init; } = [];

        public Task<IReadOnlyCollection<Booking>> ListCompletedAsync(Guid specialistId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Booking>>(Bookings
                .Where(booking => booking.SpecialistId == specialistId)
                .ToArray());
        }
    }
}
