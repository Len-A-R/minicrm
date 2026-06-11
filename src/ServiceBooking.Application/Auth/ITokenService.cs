using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Auth;

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(Specialist specialist, DateTimeOffset utcNow);

    AccessTokenResult CreateClientAccessToken(Client client, DateTimeOffset utcNow);

    AccessTokenResult CreateAdminAccessToken(AdminUser admin, DateTimeOffset utcNow);

    RefreshTokenResult CreateRefreshToken(DateTimeOffset utcNow);
}
