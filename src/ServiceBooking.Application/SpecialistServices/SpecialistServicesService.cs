using ServiceBooking.Application.Common;
using DomainSpecialistService = ServiceBooking.Domain.Entities.SpecialistService;

namespace ServiceBooking.Application.SpecialistServices;

public sealed class SpecialistServicesService(ISpecialistServiceRepository repository) : ISpecialistServicesService
{
    public async Task<ServiceResult<IReadOnlyCollection<SpecialistServiceResponse>>> ListForSpecialistAsync(
        Guid specialistId,
        CancellationToken cancellationToken)
    {
        if (!await repository.SpecialistExistsAsync(specialistId, cancellationToken))
        {
            return ServiceResult<IReadOnlyCollection<SpecialistServiceResponse>>.Failure(
                ResultStatus.NotFound,
                "specialist_not_found",
                "Specialist was not found.");
        }

        var items = await repository.ListForSpecialistAsync(specialistId, cancellationToken);
        return ServiceResult<IReadOnlyCollection<SpecialistServiceResponse>>.Success(items.Select(ToResponse).ToArray());
    }

    public async Task<ServiceResult<SpecialistServiceResponse>> CreateAsync(
        Guid specialistId,
        UpsertSpecialistServiceRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateRequestAsync(specialistId, request, null, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        try
        {
            var specialistService = new DomainSpecialistService(
                specialistId,
                request.ServiceId,
                request.Price,
                request.DurationMinutes);
            await repository.AddAsync(specialistService, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            specialistService = await repository.GetForSpecialistAsync(specialistId, specialistService.Id, cancellationToken)
                ?? specialistService;

            return ServiceResult<SpecialistServiceResponse>.Success(ToResponse(specialistService));
        }
        catch (ArgumentException exception)
        {
            return Validation("invalid_specialist_service", exception.Message);
        }
    }

    public async Task<ServiceResult<SpecialistServiceResponse>> UpdateAsync(
        Guid specialistId,
        Guid specialistServiceId,
        UpsertSpecialistServiceRequest request,
        CancellationToken cancellationToken)
    {
        var specialistService = await repository.GetForSpecialistAsync(
            specialistId,
            specialistServiceId,
            cancellationToken);
        if (specialistService is null)
        {
            return NotFound();
        }

        var validation = await ValidateRequestAsync(specialistId, request, specialistServiceId, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        try
        {
            specialistService.ChangeService(request.ServiceId);
            specialistService.SetPrice(request.Price);
            specialistService.SetDuration(request.DurationMinutes);
            await repository.SaveChangesAsync(cancellationToken);

            specialistService = await repository.GetForSpecialistAsync(specialistId, specialistServiceId, cancellationToken)
                ?? specialistService;

            return ServiceResult<SpecialistServiceResponse>.Success(ToResponse(specialistService));
        }
        catch (ArgumentException exception)
        {
            return Validation("invalid_specialist_service", exception.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteAsync(
        Guid specialistId,
        Guid specialistServiceId,
        CancellationToken cancellationToken)
    {
        var specialistService = await repository.GetForSpecialistAsync(
            specialistId,
            specialistServiceId,
            cancellationToken);
        if (specialistService is null)
        {
            return ServiceResult<bool>.Failure(
                ResultStatus.NotFound,
                "specialist_service_not_found",
                "Specialist service was not found.");
        }

        repository.Remove(specialistService);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    private async Task<ServiceResult<SpecialistServiceResponse>?> ValidateRequestAsync(
        Guid specialistId,
        UpsertSpecialistServiceRequest request,
        Guid? excludingSpecialistServiceId,
        CancellationToken cancellationToken)
    {
        if (!await repository.SpecialistExistsAsync(specialistId, cancellationToken))
        {
            return ServiceResult<SpecialistServiceResponse>.Failure(
                ResultStatus.NotFound,
                "specialist_not_found",
                "Specialist was not found.");
        }

        if (await repository.GetCatalogServiceAsync(request.ServiceId, cancellationToken) is null)
        {
            return ServiceResult<SpecialistServiceResponse>.Failure(
                ResultStatus.NotFound,
                "service_not_found",
                "Service was not found.");
        }

        if (await repository.HasDuplicateAsync(specialistId, request.ServiceId, excludingSpecialistServiceId, cancellationToken))
        {
            return ServiceResult<SpecialistServiceResponse>.Failure(
                ResultStatus.Conflict,
                "specialist_service_conflict",
                "Specialist already provides this service.");
        }

        return null;
    }

    private static ServiceResult<SpecialistServiceResponse> NotFound()
    {
        return ServiceResult<SpecialistServiceResponse>.Failure(
            ResultStatus.NotFound,
            "specialist_service_not_found",
            "Specialist service was not found.");
    }

    private static ServiceResult<SpecialistServiceResponse> Validation(string code, string message)
    {
        return ServiceResult<SpecialistServiceResponse>.Failure(ResultStatus.Validation, code, message);
    }

    private static SpecialistServiceResponse ToResponse(DomainSpecialistService specialistService)
    {
        return new SpecialistServiceResponse(
            specialistService.Id,
            specialistService.SpecialistId,
            specialistService.ServiceId,
            specialistService.Service?.Name ?? string.Empty,
            specialistService.Price,
            specialistService.DurationMinutes);
    }
}
