using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Application.Admin;

public sealed record AdminLoginRequest(string Email, string Password);

public sealed record BlockSpecialistRequest(string? Reason);

public sealed record ChangeSpecialistPlanRequest(Guid PlanId, DateOnly? ExpiresAt);

public sealed record AdminBookingStatusRequest(BookingStatus Status, decimal? ActualRevenue = null, string? RejectionReason = null);

public sealed record AdminClientUpdateRequest(string FullName, string Phone, ClientStatus Status, string? Tag);

public sealed record UpsertAdminServiceRequest(string Name, string? Description);

public sealed record UpsertAdminLocationRequest(string Name, string Address, string? Description);

public sealed record AdminSubscriptionStatusRequest(SubscriptionStatus Status);

public sealed record RenewSubscriptionRequest(DateOnly ExpiresAt);

public sealed record PaymentCreateRequest(Guid SpecialistId, Guid? SubscriptionId, decimal Amount, string Currency);

public sealed record PaymentWebhookRequest(Guid PaymentId, PaymentStatus Status, string? ExternalId, string? FailureReason);

public sealed record UpsertSystemSettingRequest(string Key, string Value, string? Description);

public sealed record UpsertAdminUserRequest(string FullName, string Email, string? Password, bool IsActive);

public sealed record UpsertSubscriptionPlanRequest(
    string Name,
    string? Description,
    decimal MonthlyPrice,
    int BookingLimit,
    int ServiceLimit,
    bool IsActive);
