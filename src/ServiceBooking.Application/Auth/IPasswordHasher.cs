namespace ServiceBooking.Application.Auth;

public interface IPasswordHasher
{
    string Hash(string value);

    bool Verify(string value, string hash);
}
