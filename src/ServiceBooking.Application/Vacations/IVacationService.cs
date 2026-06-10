using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Vacations;

public interface IVacationService
{
    Task<ServiceResult<IReadOnlyCollection<VacationResponse>>> ListForSpecialistAsync(
        Guid specialistId,
        CancellationToken cancellationToken);

    Task<ServiceResult<VacationResponse>> CreateAsync(
        Guid specialistId,
        UpsertVacationRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<VacationResponse>> UpdateAsync(
        Guid specialistId,
        Guid vacationId,
        UpsertVacationRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteAsync(Guid specialistId, Guid vacationId, CancellationToken cancellationToken);
}
