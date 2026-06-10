namespace ServiceBooking.Application.Auth;

public sealed record RefreshTokenRequest(Guid SpecialistId, string RefreshToken);
