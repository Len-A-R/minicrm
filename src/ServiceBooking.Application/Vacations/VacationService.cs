using ServiceBooking.Application.Common;
using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Vacations;

public sealed class VacationService(IVacationRepository repository) : IVacationService
{
    public async Task<ServiceResult<IReadOnlyCollection<VacationResponse>>> ListForSpecialistAsync(
        Guid specialistId,
        CancellationToken cancellationToken)
    {
        if (!await repository.SpecialistExistsAsync(specialistId, cancellationToken))
        {
            return ServiceResult<IReadOnlyCollection<VacationResponse>>.Failure(
                ResultStatus.NotFound,
                "specialist_not_found",
                "Specialist was not found.");
        }

        var vacations = await repository.ListForSpecialistAsync(specialistId, cancellationToken);
        return ServiceResult<IReadOnlyCollection<VacationResponse>>.Success(vacations.Select(ToResponse).ToArray());
    }

    public async Task<ServiceResult<VacationResponse>> CreateAsync(
        Guid specialistId,
        UpsertVacationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(specialistId, request, null, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        try
        {
            var vacation = new Vacation(specialistId, request.Date, request.Reason);
            await repository.AddAsync(vacation, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return ServiceResult<VacationResponse>.Success(ToResponse(vacation));
        }
        catch (ArgumentException exception)
        {
            return Validation("invalid_vacation", exception.Message);
        }
    }

    public async Task<ServiceResult<VacationResponse>> UpdateAsync(
        Guid specialistId,
        Guid vacationId,
        UpsertVacationRequest request,
        CancellationToken cancellationToken)
    {
        var vacation = await repository.GetForSpecialistAsync(specialistId, vacationId, cancellationToken);
        if (vacation is null)
        {
            return NotFound();
        }

        var validation = await ValidateAsync(specialistId, request, vacationId, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        try
        {
            vacation.ChangeDate(request.Date);
            vacation.SetReason(request.Reason);
            await repository.SaveChangesAsync(cancellationToken);
            return ServiceResult<VacationResponse>.Success(ToResponse(vacation));
        }
        catch (ArgumentException exception)
        {
            return Validation("invalid_vacation", exception.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteAsync(
        Guid specialistId,
        Guid vacationId,
        CancellationToken cancellationToken)
    {
        var vacation = await repository.GetForSpecialistAsync(specialistId, vacationId, cancellationToken);
        if (vacation is null)
        {
            return ServiceResult<bool>.Failure(
                ResultStatus.NotFound,
                "vacation_not_found",
                "Vacation was not found.");
        }

        repository.Remove(vacation);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    private async Task<ServiceResult<VacationResponse>?> ValidateAsync(
        Guid specialistId,
        UpsertVacationRequest request,
        Guid? excludingVacationId,
        CancellationToken cancellationToken)
    {
        if (!await repository.SpecialistExistsAsync(specialistId, cancellationToken))
        {
            return ServiceResult<VacationResponse>.Failure(
                ResultStatus.NotFound,
                "specialist_not_found",
                "Specialist was not found.");
        }

        if (await repository.HasDuplicateDateAsync(specialistId, request.Date, excludingVacationId, cancellationToken))
        {
            return ServiceResult<VacationResponse>.Failure(
                ResultStatus.Conflict,
                "vacation_conflict",
                "Vacation already exists for this date.");
        }

        return null;
    }

    private static ServiceResult<VacationResponse> NotFound()
    {
        return ServiceResult<VacationResponse>.Failure(
            ResultStatus.NotFound,
            "vacation_not_found",
            "Vacation was not found.");
    }

    private static ServiceResult<VacationResponse> Validation(string code, string message)
    {
        return ServiceResult<VacationResponse>.Failure(ResultStatus.Validation, code, message);
    }

    private static VacationResponse ToResponse(Vacation vacation)
    {
        return new VacationResponse(vacation.Id, vacation.SpecialistId, vacation.Date, vacation.Reason);
    }
}
