using ServiceBooking.Application.Common;
using CatalogService = ServiceBooking.Domain.Entities.Service;

namespace ServiceBooking.Application.Catalog;

public sealed class ServiceCatalogService(IServiceCatalogRepository services) : IServiceCatalogService
{
    public async Task<ServiceResult<IReadOnlyCollection<ServiceResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        var items = await services.ListAsync(cancellationToken);
        return ServiceResult<IReadOnlyCollection<ServiceResponse>>.Success(items.Select(ToResponse).ToArray());
    }

    public async Task<ServiceResult<ServiceResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Validation("invalid_service_id", "Service id must be a non-empty UUID.");
        }

        var service = await services.GetByIdAsync(id, cancellationToken);
        return service is null ? NotFound() : ServiceResult<ServiceResponse>.Success(ToResponse(service));
    }

    public async Task<ServiceResult<ServiceResponse>> CreateAsync(
        UpsertServiceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var service = new CatalogService(request.Name, request.Description);
            await services.AddAsync(service, cancellationToken);
            await services.SaveChangesAsync(cancellationToken);
            return ServiceResult<ServiceResponse>.Success(ToResponse(service));
        }
        catch (ArgumentException exception)
        {
            return Validation("invalid_service", exception.Message);
        }
    }

    public async Task<ServiceResult<ServiceResponse>> UpdateAsync(
        Guid id,
        UpsertServiceRequest request,
        CancellationToken cancellationToken)
    {
        var service = await services.GetByIdAsync(id, cancellationToken);
        if (service is null)
        {
            return NotFound();
        }

        try
        {
            service.Rename(request.Name);
            service.SetDescription(request.Description);
            await services.SaveChangesAsync(cancellationToken);
            return ServiceResult<ServiceResponse>.Success(ToResponse(service));
        }
        catch (ArgumentException exception)
        {
            return Validation("invalid_service", exception.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var service = await services.GetByIdAsync(id, cancellationToken);
        if (service is null)
        {
            return ServiceResult<bool>.Failure(
                ResultStatus.NotFound,
                "service_not_found",
                "Service was not found.");
        }

        if (await services.HasSpecialistServicesAsync(id, cancellationToken))
        {
            return ServiceResult<bool>.Failure(
                ResultStatus.Conflict,
                "service_in_use",
                "Service is used by specialists and cannot be deleted.");
        }

        services.Remove(service);
        await services.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    private static ServiceResult<ServiceResponse> NotFound()
    {
        return ServiceResult<ServiceResponse>.Failure(
            ResultStatus.NotFound,
            "service_not_found",
            "Service was not found.");
    }

    private static ServiceResult<ServiceResponse> Validation(string code, string message)
    {
        return ServiceResult<ServiceResponse>.Failure(ResultStatus.Validation, code, message);
    }

    private static ServiceResponse ToResponse(CatalogService service)
    {
        return new ServiceResponse(service.Id, service.Name, service.Description);
    }
}
