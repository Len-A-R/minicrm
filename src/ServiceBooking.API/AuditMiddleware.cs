using System.Security.Claims;
using ServiceBooking.Application.Admin;

namespace ServiceBooking.API;

public sealed class AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
    {
        await next(context);

        if (!ShouldAudit(context.Request))
        {
            return;
        }

        try
        {
            var actorId = GetActorId(context.User);
            var actorType = context.User.IsInRole("Admin")
                ? "Admin"
                : context.User.IsInRole("Specialist")
                    ? "Specialist"
                    : "Anonymous";
            var path = context.Request.Path.Value ?? string.Empty;
            var entityType = ResolveEntityType(path);
            var entityId = ResolveEntityId(path);
            var outcome = context.Response.StatusCode < 400 ? "Success" : "Failure";

            await auditLogService.RecordAsync(
                actorId,
                actorType,
                $"{context.Request.Method} {path}",
                entityType,
                entityId,
                outcome,
                $"HTTP {context.Response.StatusCode}",
                context.Connection.RemoteIpAddress?.ToString(),
                context.RequestAborted);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to write audit log for {Method} {Path}", context.Request.Method, context.Request.Path);
        }
    }

    private static bool ShouldAudit(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api/v1", StringComparison.OrdinalIgnoreCase)
            && !HttpMethods.IsGet(request.Method)
            && !HttpMethods.IsHead(request.Method)
            && !HttpMethods.IsOptions(request.Method);
    }

    private static Guid? GetActorId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var actorId) ? actorId : null;
    }

    private static string ResolveEntityType(string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4 && parts[2].Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            return parts[3];
        }

        return parts.Length >= 3 ? parts[2] : "api";
    }

    private static string? ResolveEntityId(string path)
    {
        return path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(part => Guid.TryParse(part, out _));
    }
}
