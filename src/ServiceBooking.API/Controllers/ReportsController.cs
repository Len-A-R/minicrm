using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Reports;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Authorize(Roles = "Specialist")]
[Route("api/v1/reports")]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType<ReportSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ReportSummaryResponse>> Summary(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await reportService.GetSummaryAsync(specialistId.Value, new ReportPeriodQuery(from, to), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpGet("by-service")]
    [ProducesResponseType<IReadOnlyCollection<RevenueByServiceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<RevenueByServiceResponse>>> ByService(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await reportService.GetByServiceAsync(specialistId.Value, new ReportPeriodQuery(from, to), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpGet("by-client")]
    [ProducesResponseType<IReadOnlyCollection<RevenueByClientResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<RevenueByClientResponse>>> ByClient(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await reportService.GetByClientAsync(specialistId.Value, new ReportPeriodQuery(from, to), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpGet("by-day")]
    [ProducesResponseType<IReadOnlyCollection<RevenueByDayResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<RevenueByDayResponse>>> ByDay(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await reportService.GetByDayAsync(specialistId.Value, new ReportPeriodQuery(from, to), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }
}
