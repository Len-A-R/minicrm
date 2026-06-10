using ServiceBooking.Application.Common;
using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Specialists;

public sealed class PublicSpecialistService(IPublicSpecialistRepository specialists) : IPublicSpecialistService
{
    public async Task<ServiceResult<IReadOnlyCollection<PublicSpecialistResponse>>> ListAsync(
        Guid locationId,
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        if (locationId == Guid.Empty)
        {
            return ServiceResult<IReadOnlyCollection<PublicSpecialistResponse>>.Failure(
                ResultStatus.Validation,
                "invalid_location_id",
                "Location id is required.");
        }

        if (serviceId == Guid.Empty)
        {
            return ServiceResult<IReadOnlyCollection<PublicSpecialistResponse>>.Failure(
                ResultStatus.Validation,
                "invalid_service_id",
                "Service id is required.");
        }

        var items = await specialists.ListAsync(locationId, serviceId, cancellationToken);
        return ServiceResult<IReadOnlyCollection<PublicSpecialistResponse>>.Success(items.Select(ToResponse).ToArray());
    }

    private static PublicSpecialistResponse ToResponse(Specialist specialist)
    {
        return new PublicSpecialistResponse(
            specialist.Id,
            specialist.FullName,
            specialist.AvatarUrl,
            specialist.VenueName,
            specialist.LocationId,
            specialist.Location?.Name);
    }
}
