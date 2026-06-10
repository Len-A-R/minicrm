using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.SpecialistClients;

public interface ISpecialistClientService
{
    Task<ServiceResult<IReadOnlyCollection<SpecialistClientResponse>>> ListAsync(
        Guid specialistId,
        CancellationToken cancellationToken);

    Task<ServiceResult<SpecialistClientResponse>> GetByIdAsync(
        Guid specialistId,
        Guid clientId,
        CancellationToken cancellationToken);

    Task<ServiceResult<SpecialistClientResponse>> UpdateStatusAsync(
        Guid specialistId,
        Guid clientId,
        UpdateClientStatusRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<SpecialistClientResponse>> UpdateTagAsync(
        Guid specialistId,
        Guid clientId,
        UpdateClientTagRequest request,
        CancellationToken cancellationToken);
}
