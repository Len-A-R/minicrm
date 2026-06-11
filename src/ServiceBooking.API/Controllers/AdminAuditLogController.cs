using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Admin;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/audit-logs")]
public sealed class AdminAuditLogController(IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AdminAuditLogResponse>>> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? actorId,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        CancellationToken cancellationToken)
    {
        var result = await auditLogService.ListAsync(from, to, actorId, action, entityType, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpGet("export.csv")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? actorId,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        CancellationToken cancellationToken)
    {
        var result = await auditLogService.ExportCsvAsync(from, to, actorId, action, entityType, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToErrorResult(result).Result ?? BadRequest(new ErrorResponse("export_failed", "Audit export failed."));
        }

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(result.Value!)).ToArray(), "text/csv", "audit-logs.csv");
    }
}
