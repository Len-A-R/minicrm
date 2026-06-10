namespace ServiceBooking.Application.SpecialistServices;

public sealed record UpsertSpecialistServiceRequest(Guid ServiceId, decimal Price, int DurationMinutes);
