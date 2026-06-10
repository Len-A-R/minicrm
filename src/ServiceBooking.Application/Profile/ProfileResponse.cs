namespace ServiceBooking.Application.Profile;

public sealed record ProfileResponse(
    Guid Id,
    string FullName,
    string Email,
    string Phone,
    string? AvatarUrl,
    string? VenueName,
    Guid? LocationId,
    DateTimeOffset CreatedAt);
