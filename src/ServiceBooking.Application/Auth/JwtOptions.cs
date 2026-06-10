namespace ServiceBooking.Application.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "ServiceBooking";
    public string Audience { get; init; } = "ServiceBooking";
    public string SigningKey { get; init; } = "ServiceBooking-development-signing-key-change-me";
    public int AccessTokenMinutes { get; init; } = 30;
    public int RefreshTokenDays { get; init; } = 30;
}
