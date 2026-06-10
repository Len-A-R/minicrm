namespace ServiceBooking.Application.Auth;

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);
