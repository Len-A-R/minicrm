namespace ServiceBooking.Application.Profile;

public sealed record UpdateSpecialistProfileRequest(
    string FullName,
    string Phone,
    string? VenueName,
    Guid? LocationId);
