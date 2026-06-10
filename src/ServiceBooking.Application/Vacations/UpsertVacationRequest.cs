namespace ServiceBooking.Application.Vacations;

public sealed record UpsertVacationRequest(DateOnly Date, string? Reason);
