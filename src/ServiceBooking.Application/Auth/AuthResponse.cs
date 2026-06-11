namespace ServiceBooking.Application.Auth;

public sealed record AuthResponse(
    Guid SpecialistId,
    string FullName,
    string Email,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt)
{
    public string Role { get; init; } = "Specialist";

    public Guid? ClientId { get; init; }

    public Guid? AdminId { get; init; }
}
