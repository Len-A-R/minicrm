using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.SpecialistServices;

public interface ISpecialistServicesService
{
    Task<ServiceResult<IReadOnlyCollection<SpecialistServiceResponse>>> ListForSpecialistAsync(
        Guid specialistId,
        CancellationToken cancellationToken);

    Task<ServiceResult<SpecialistServiceResponse>> CreateAsync(
        Guid specialistId,
        UpsertSpecialistServiceRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<SpecialistServiceResponse>> UpdateAsync(
        Guid specialistId,
        Guid specialistServiceId,
        UpsertSpecialistServiceRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteAsync(
        Guid specialistId,
        Guid specialistServiceId,
        CancellationToken cancellationToken);
}
