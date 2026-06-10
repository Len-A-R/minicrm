namespace ServiceBooking.Application.SpecialistBookings;

public sealed record ConfirmBookingRequest(DateOnly Date, TimeOnly Time);

public sealed record RejectBookingRequest(string? Reason);

public sealed record CompleteBookingRequest(decimal ActualRevenue);

public sealed record ReplyBookingRequest(string Reply);

public sealed record BookingListQuery(
    string? Status,
    DateOnly? Date,
    string? Search,
    int Page = 1,
    int PageSize = 20);
