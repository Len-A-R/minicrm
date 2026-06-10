using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Specialists;

public interface IPublicSpecialistService
{
    Task<ServiceResult<IReadOnlyCollection<PublicSpecialistResponse>>> ListAsync(
        Guid locationId,
        Guid serviceId,
        CancellationToken cancellationToken);
}
