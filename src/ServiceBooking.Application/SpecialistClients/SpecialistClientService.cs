using ServiceBooking.Application.Common;
using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.SpecialistClients;

public sealed class SpecialistClientService(ISpecialistClientRepository clients) : ISpecialistClientService
{
    public async Task<ServiceResult<IReadOnlyCollection<SpecialistClientResponse>>> ListAsync(
        Guid specialistId,
        CancellationToken cancellationToken)
    {
        if (specialistId == Guid.Empty)
        {
            return Validation<IReadOnlyCollection<SpecialistClientResponse>>("invalid_specialist_id", "Specialist id is required.");
        }

        var items = await clients.ListAsync(specialistId, cancellationToken);
        return ServiceResult<IReadOnlyCollection<SpecialistClientResponse>>.Success(items.Select(ToResponse).ToArray());
    }

    public async Task<ServiceResult<SpecialistClientResponse>> GetByIdAsync(
        Guid specialistId,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var client = await GetClientOrFailureAsync(specialistId, clientId, cancellationToken);
        return client.IsSuccess
            ? ServiceResult<SpecialistClientResponse>.Success(ToResponse(client.Value!))
            : FailureFromClientResult(client);
    }

    public async Task<ServiceResult<SpecialistClientResponse>> UpdateStatusAsync(
        Guid specialistId,
        Guid clientId,
        UpdateClientStatusRequest request,
        CancellationToken cancellationToken)
    {
        var client = await GetClientOrFailureAsync(specialistId, clientId, cancellationToken);
        if (!client.IsSuccess)
        {
            return FailureFromClientResult(client);
        }

        client.Value!.ChangeStatus(request.Status);
        await clients.SaveChangesAsync(cancellationToken);
        return ServiceResult<SpecialistClientResponse>.Success(ToResponse(client.Value));
    }

    public async Task<ServiceResult<SpecialistClientResponse>> UpdateTagAsync(
        Guid specialistId,
        Guid clientId,
        UpdateClientTagRequest request,
        CancellationToken cancellationToken)
    {
        var client = await GetClientOrFailureAsync(specialistId, clientId, cancellationToken);
        if (!client.IsSuccess)
        {
            return FailureFromClientResult(client);
        }

        try
        {
            client.Value!.SetTag(request.Tag);
            await clients.SaveChangesAsync(cancellationToken);
            return ServiceResult<SpecialistClientResponse>.Success(ToResponse(client.Value));
        }
        catch (ArgumentException exception)
        {
            return Validation<SpecialistClientResponse>("invalid_tag", exception.Message);
        }
    }

    private async Task<ServiceResult<Client>> GetClientOrFailureAsync(
        Guid specialistId,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        if (specialistId == Guid.Empty || clientId == Guid.Empty)
        {
            return Validation<Client>("invalid_client_id", "Client id is required.");
        }

        var client = await clients.GetByIdAsync(specialistId, clientId, cancellationToken);
        return client is null
            ? ServiceResult<Client>.Failure(ResultStatus.NotFound, "client_not_found", "Client was not found.")
            : ServiceResult<Client>.Success(client);
    }

    private static SpecialistClientResponse ToResponse(Client client)
    {
        var bookingCount = client.Bookings.Count;
        var lastBookingAt = client.Bookings
            .OrderByDescending(booking => booking.CreatedAt)
            .Select(booking => (DateTimeOffset?)booking.CreatedAt)
            .FirstOrDefault();

        return new SpecialistClientResponse(
            client.Id,
            client.FullName,
            client.Phone,
            client.Status,
            client.Tag,
            bookingCount,
            lastBookingAt);
    }

    private static ServiceResult<T> Validation<T>(string code, string message)
    {
        return ServiceResult<T>.Failure(ResultStatus.Validation, code, message);
    }

    private static ServiceResult<SpecialistClientResponse> FailureFromClientResult(ServiceResult<Client> result)
    {
        return ServiceResult<SpecialistClientResponse>.Failure(
            result.Status,
            result.Error!.Code,
            result.Error.Message);
    }
}
