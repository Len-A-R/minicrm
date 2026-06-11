using System.Text;
using ServiceBooking.Application.Common;
using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Admin;

public sealed class AuditLogService(
    IAdminRepository repository,
    IDateTimeProvider dateTimeProvider) : IAuditLogService
{
    public async Task<ServiceResult<AdminAuditLogResponse>> RecordAsync(
        Guid? actorId,
        string actorType,
        string action,
        string entityType,
        string? entityId,
        string outcome,
        string? details,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        try
        {
            var log = new AuditLog(
                actorId,
                actorType,
                action,
                entityType,
                entityId,
                outcome,
                details,
                ipAddress,
                dateTimeProvider.UtcNow);

            await repository.AddAuditLogAsync(log, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return ServiceResult<AdminAuditLogResponse>.Success(ToResponse(log));
        }
        catch (ArgumentException exception)
        {
            return ServiceResult<AdminAuditLogResponse>.Failure(ResultStatus.Validation, "invalid_audit_log", exception.Message);
        }
    }

    public async Task<ServiceResult<IReadOnlyCollection<AdminAuditLogResponse>>> ListAsync(
        DateOnly? from,
        DateOnly? to,
        Guid? actorId,
        string? action,
        string? entityType,
        CancellationToken cancellationToken)
    {
        var logs = await FilterAsync(from, to, actorId, action, entityType, cancellationToken);
        return ServiceResult<IReadOnlyCollection<AdminAuditLogResponse>>.Success(logs.Select(ToResponse).ToArray());
    }

    public async Task<ServiceResult<string>> ExportCsvAsync(
        DateOnly? from,
        DateOnly? to,
        Guid? actorId,
        string? action,
        string? entityType,
        CancellationToken cancellationToken)
    {
        var logs = await FilterAsync(from, to, actorId, action, entityType, cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("Дата;Актор;Тип актора;Действие;Сущность;ID сущности;Результат;IP;Детали");
        foreach (var log in logs)
        {
            csv.AppendLine(string.Join(
                ';',
                Csv(log.CreatedAt.ToString("O")),
                Csv(log.ActorId?.ToString() ?? string.Empty),
                Csv(log.ActorType),
                Csv(log.Action),
                Csv(log.EntityType),
                Csv(log.EntityId ?? string.Empty),
                Csv(log.Outcome),
                Csv(log.IpAddress ?? string.Empty),
                Csv(log.Details ?? string.Empty)));
        }

        return ServiceResult<string>.Success(csv.ToString());
    }

    private async Task<IReadOnlyCollection<AuditLog>> FilterAsync(
        DateOnly? from,
        DateOnly? to,
        Guid? actorId,
        string? action,
        string? entityType,
        CancellationToken cancellationToken)
    {
        var logs = await repository.ListAuditLogsAsync(cancellationToken);
        var query = logs.AsEnumerable();
        if (from.HasValue)
        {
            query = query.Where(log => DateOnly.FromDateTime(log.CreatedAt.DateTime) >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(log => DateOnly.FromDateTime(log.CreatedAt.DateTime) <= to.Value);
        }

        if (actorId.HasValue)
        {
            query = query.Where(log => log.ActorId == actorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(log => log.Action.Contains(action.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(log => log.EntityType.Equals(entityType.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderByDescending(log => log.CreatedAt).ToArray();
    }

    private static AdminAuditLogResponse ToResponse(AuditLog log)
    {
        return new AdminAuditLogResponse(
            log.Id,
            log.ActorId,
            log.ActorType,
            log.Action,
            log.EntityType,
            log.EntityId,
            log.Outcome,
            log.Details,
            log.IpAddress,
            log.CreatedAt);
    }

    private static string Csv(string value)
    {
        return value.Contains(';', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal) || value.Contains('\n', StringComparison.Ordinal)
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }
}
