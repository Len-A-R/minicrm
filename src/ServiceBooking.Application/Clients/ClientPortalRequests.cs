namespace ServiceBooking.Application.Clients;

public sealed record CreateClientBookingRequest(
    Guid SpecialistId,
    IReadOnlyCollection<Guid> ServiceIds,
    DateOnly RequestedDate,
    TimeOnly RequestedTime,
    string? Message);

public sealed record UpdateClientProfileRequest(string FullName, string Phone);
