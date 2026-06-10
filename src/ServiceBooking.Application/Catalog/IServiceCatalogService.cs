using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Catalog;

public interface IServiceCatalogService
{
    Task<ServiceResult<IReadOnlyCollection<ServiceResponse>>> ListAsync(CancellationToken cancellationToken);

    Task<ServiceResult<ServiceResponse>> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<ServiceResponse>> CreateAsync(UpsertServiceRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<ServiceResponse>> UpdateAsync(
        Guid id,
        UpsertServiceRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
