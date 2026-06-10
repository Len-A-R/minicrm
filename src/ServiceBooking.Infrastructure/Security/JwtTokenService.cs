using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServiceBooking.Application.Auth;
using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public AccessTokenResult CreateAccessToken(Specialist specialist, DateTimeOffset utcNow)
    {
        var expiresAt = utcNow.AddMinutes(_options.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, specialist.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, specialist.Id.ToString()),
            new Claim(ClaimTypes.Email, specialist.Email),
            new Claim(ClaimTypes.Name, specialist.FullName),
            new Claim(ClaimTypes.Role, "Specialist")
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            notBefore: utcNow.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public RefreshTokenResult CreateRefreshToken(DateTimeOffset utcNow)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');

        return new RefreshTokenResult(token, utcNow.AddDays(_options.RefreshTokenDays));
    }
}
