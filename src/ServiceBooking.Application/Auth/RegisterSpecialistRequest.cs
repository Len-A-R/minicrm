namespace ServiceBooking.Application.Auth;

public sealed record RegisterSpecialistRequest(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword,
    string Phone);
