namespace ServiceBooking.Application.SpecialistServices;

public sealed record SpecialistServiceResponse(
    Guid Id,
    Guid SpecialistId,
    Guid ServiceId,
    string ServiceName,
    decimal Price,
    int DurationMinutes);
