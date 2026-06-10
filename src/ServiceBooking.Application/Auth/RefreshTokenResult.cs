namespace ServiceBooking.Application.Auth;

public sealed record RefreshTokenResult(string Token, DateTimeOffset ExpiresAt);
