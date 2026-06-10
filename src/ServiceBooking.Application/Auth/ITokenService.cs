using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Auth;

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(Specialist specialist, DateTimeOffset utcNow);

    RefreshTokenResult CreateRefreshToken(DateTimeOffset utcNow);
}
