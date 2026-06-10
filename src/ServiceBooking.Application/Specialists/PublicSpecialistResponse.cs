namespace ServiceBooking.Application.Specialists;

public sealed record PublicSpecialistResponse(
    Guid Id,
    string FullName,
    string? AvatarUrl,
    string? VenueName,
    Guid? LocationId,
    string? LocationName);
