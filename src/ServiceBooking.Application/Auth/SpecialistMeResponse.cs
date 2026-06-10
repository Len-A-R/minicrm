namespace ServiceBooking.Application.Auth;

public sealed record SpecialistMeResponse(
    Guid Id,
    string FullName,
    string Email,
    string Phone,
    string? AvatarUrl,
    string? VenueName,
    Guid? LocationId);
