using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Admin;

public interface IAuditLogService
{
    Task<ServiceResult<AdminAuditLogResponse>> RecordAsync(
        Guid? actorId,
        string actorType,
        string action,
        string entityType,
        string? entityId,
        string outcome,
        string? details,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<AdminAuditLogResponse>>> ListAsync(
        DateOnly? from,
        DateOnly? to,
        Guid? actorId,
        string? action,
        string? entityType,
        CancellationToken cancellationToken);

    Task<ServiceResult<string>> ExportCsvAsync(
        DateOnly? from,
        DateOnly? to,
        Guid? actorId,
        string? action,
        string? entityType,
        CancellationToken cancellationToken);
}
