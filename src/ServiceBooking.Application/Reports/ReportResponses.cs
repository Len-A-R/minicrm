namespace ServiceBooking.Application.Reports;

public sealed record ReportSummaryResponse(
    DateOnly From,
    DateOnly To,
    decimal TotalRevenue,
    int CompletedBookings,
    decimal AverageCheck);

public sealed record RevenueByServiceResponse(
    Guid ServiceId,
    string ServiceName,
    decimal Revenue,
    int CompletedBookings,
    int Quantity);

public sealed record RevenueByClientResponse(
    Guid? ClientId,
    string ClientName,
    string ClientPhone,
    decimal Revenue,
    int CompletedBookings);

public sealed record RevenueByDayResponse(
    DateOnly Date,
    decimal Revenue,
    int CompletedBookings,
    decimal AverageCheck);
