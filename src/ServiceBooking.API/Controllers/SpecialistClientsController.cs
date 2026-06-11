using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.SpecialistClients;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Authorize(Roles = "Specialist")]
[Route("api/v1/specialist/clients")]
public sealed class SpecialistClientsController(ISpecialistClientService clientService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<SpecialistClientResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<SpecialistClientResponse>>> List(CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await clientService.ListAsync(specialistId.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<SpecialistClientResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpecialistClientResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await clientService.GetByIdAsync(specialistId.Value, id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType<SpecialistClientResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpecialistClientResponse>> UpdateStatus(
        Guid id,
        UpdateClientStatusRequest request,
        CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await clientService.UpdateStatusAsync(specialistId.Value, id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }

    [HttpPut("{id:guid}/tag")]
    [ProducesResponseType<SpecialistClientResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpecialistClientResponse>> UpdateTag(
        Guid id,
        UpdateClientTagRequest request,
        CancellationToken cancellationToken)
    {
        var specialistId = User.GetCurrentSpecialistId();
        if (specialistId is null)
        {
            return Unauthorized(new ErrorResponse("invalid_token", "Access token does not contain specialist id."));
        }

        var result = await clientService.UpdateTagAsync(specialistId.Value, id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }
}
