namespace ServiceBooking.Application.Auth;

public sealed record RegisterClientRequest(
    string FullName,
    string Email,
    string Phone,
    string Password,
    string ConfirmPassword);
