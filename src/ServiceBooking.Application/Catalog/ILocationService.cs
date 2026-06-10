using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Catalog;

public interface ILocationService
{
    Task<ServiceResult<IReadOnlyCollection<LocationResponse>>> ListAsync(
        Guid? serviceId,
        CancellationToken cancellationToken);

    Task<ServiceResult<LocationResponse>> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<LocationResponse>> CreateAsync(UpsertLocationRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<LocationResponse>> UpdateAsync(
        Guid id,
        UpsertLocationRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
