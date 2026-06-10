using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Application.Specialists;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Route("api/v1/specialists")]
public sealed class SpecialistsController(IPublicSpecialistService specialistService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<PublicSpecialistResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<PublicSpecialistResponse>>> List(
        [FromQuery] Guid locationId,
        [FromQuery] Guid serviceId,
        CancellationToken cancellationToken)
    {
        var result = await specialistService.ListAsync(locationId, serviceId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToErrorResult(result);
    }
}
