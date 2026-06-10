using ServiceBooking.Application.Auth;

namespace ServiceBooking.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return BCrypt.Net.BCrypt.HashPassword(value);
    }

    public bool Verify(string value, string hash)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(value, hash);
    }
}
