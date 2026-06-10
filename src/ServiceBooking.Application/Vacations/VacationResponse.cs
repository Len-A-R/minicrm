namespace ServiceBooking.Application.Vacations;

public sealed record VacationResponse(Guid Id, Guid SpecialistId, DateOnly Date, string? Reason);
