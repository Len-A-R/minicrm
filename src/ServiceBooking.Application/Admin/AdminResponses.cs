using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Application.Admin;

public sealed record AdminAuthResponse(
    Guid AdminId,
    string FullName,
    string Email,
    string AccessToken,
    DateTimeOffset ExpiresAt);

public sealed record AdminMeResponse(Guid AdminId, string FullName, string Email, bool IsActive);

public sealed record AdminUserResponse(
    Guid Id,
    string FullName,
    string Email,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);

public sealed record SubscriptionPlanResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal MonthlyPrice,
    int BookingLimit,
    int ServiceLimit,
    bool IsActive);

public sealed record AdminSpecialistResponse(
    Guid Id,
    string FullName,
    string Email,
    string Phone,
    string? VenueName,
    Guid? LocationId,
    bool IsBlocked,
    string? BlockReason,
    DateTimeOffset CreatedAt,
    string? SubscriptionPlanName,
    SubscriptionStatus? SubscriptionStatus,
    DateTimeOffset? SubscriptionExpiresAt);

public sealed record AdminBookingResponse(
    Guid Id,
    Guid SpecialistId,
    string SpecialistName,
    string ClientName,
    string ClientPhone,
    DateOnly RequestedDate,
    TimeOnly RequestedTime,
    decimal TotalPrice,
    BookingStatus Status,
    DateTimeOffset CreatedAt,
    string ServicesSummary);

public sealed record AdminClientResponse(
    Guid Id,
    string FullName,
    string Phone,
    ClientStatus Status,
    string? Tag,
    int BookingCount,
    DateTimeOffset? LastBookingAt);

public sealed record AdminServiceResponse(Guid Id, string Name, string? Description);

public sealed record AdminLocationResponse(Guid Id, string Name, string Address, string? Description);

public sealed record AdminPaymentResponse(
    Guid Id,
    Guid SpecialistId,
    string SpecialistName,
    Guid? SubscriptionId,
    decimal Amount,
    string Currency,
    string Provider,
    PaymentStatus Status,
    string? ExternalId,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt);

public sealed record AdminSubscriptionResponse(
    Guid Id,
    Guid SpecialistId,
    string SpecialistName,
    Guid PlanId,
    string PlanName,
    SubscriptionStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RenewedAt);

public sealed record AdminAuditLogResponse(
    Guid Id,
    Guid? ActorId,
    string ActorType,
    string Action,
    string EntityType,
    string? EntityId,
    string Outcome,
    string? Details,
    string? IpAddress,
    DateTimeOffset CreatedAt);

public sealed record AdminSettingResponse(Guid Id, string Key, string Value, string? Description, DateTimeOffset UpdatedAt);

public sealed record PlatformFinanceSummaryResponse(decimal Mrr, decimal Arpu, int PaidSpecialists, decimal TotalRevenue);
