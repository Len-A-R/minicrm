using ServiceBooking.Application.Common;
using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Catalog;

public sealed class LocationService(ILocationRepository locations) : ILocationService
{
    public async Task<ServiceResult<IReadOnlyCollection<LocationResponse>>> ListAsync(
        Guid? serviceId,
        CancellationToken cancellationToken)
    {
        if (serviceId == Guid.Empty)
        {
            return ServiceResult<IReadOnlyCollection<LocationResponse>>.Failure(
                ResultStatus.Validation,
                "invalid_service_id",
                "Service id must be a non-empty UUID.");
        }

        var items = await locations.ListAsync(serviceId, cancellationToken);
        return ServiceResult<IReadOnlyCollection<LocationResponse>>.Success(items.Select(ToResponse).ToArray());
    }

    public async Task<ServiceResult<LocationResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Validation("invalid_location_id", "Location id must be a non-empty UUID.");
        }

        var location = await locations.GetByIdAsync(id, cancellationToken);
        return location is null ? NotFound() : ServiceResult<LocationResponse>.Success(ToResponse(location));
    }

    public async Task<ServiceResult<LocationResponse>> CreateAsync(
        UpsertLocationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var location = new Location(request.Name, request.Address, request.Description);
            await locations.AddAsync(location, cancellationToken);
            await locations.SaveChangesAsync(cancellationToken);
            return ServiceResult<LocationResponse>.Success(ToResponse(location));
        }
        catch (ArgumentException exception)
        {
            return Validation("invalid_location", exception.Message);
        }
    }

    public async Task<ServiceResult<LocationResponse>> UpdateAsync(
        Guid id,
        UpsertLocationRequest request,
        CancellationToken cancellationToken)
    {
        var location = await locations.GetByIdAsync(id, cancellationToken);
        if (location is null)
        {
            return NotFound();
        }

        try
        {
            location.Rename(request.Name);
            location.ChangeAddress(request.Address);
            location.SetDescription(request.Description);
            await locations.SaveChangesAsync(cancellationToken);
            return ServiceResult<LocationResponse>.Success(ToResponse(location));
        }
        catch (ArgumentException exception)
        {
            return Validation("invalid_location", exception.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var location = await locations.GetByIdAsync(id, cancellationToken);
        if (location is null)
        {
            return ServiceResult<bool>.Failure(
                ResultStatus.NotFound,
                "location_not_found",
                "Location was not found.");
        }

        locations.Remove(location);
        await locations.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    private static ServiceResult<LocationResponse> NotFound()
    {
        return ServiceResult<LocationResponse>.Failure(
            ResultStatus.NotFound,
            "location_not_found",
            "Location was not found.");
    }

    private static ServiceResult<LocationResponse> Validation(string code, string message)
    {
        return ServiceResult<LocationResponse>.Failure(ResultStatus.Validation, code, message);
    }

    private static LocationResponse ToResponse(Location location)
    {
        return new LocationResponse(location.Id, location.Name, location.Address, location.Description);
    }
}
