using ServiceBooking.Application.Bookings;
using ServiceBooking.Application.Common;
using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Clients;

public sealed class ClientPortalService(
    IClientPortalRepository repository,
    IBookingService bookingService) : IClientPortalService
{
    public async Task<ServiceResult<ClientMeResponse>> GetMeAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var client = await GetClientOrFailureAsync(clientId, cancellationToken);
        return client.IsSuccess
            ? ServiceResult<ClientMeResponse>.Success(ToMeResponse(client.Value!))
            : ServiceResult<ClientMeResponse>.Failure(client.Status, client.Error!.Code, client.Error.Message);
    }

    public async Task<ServiceResult<ClientMeResponse>> UpdateMeAsync(
        Guid clientId,
        UpdateClientProfileRequest request,
        CancellationToken cancellationToken)
    {
        var clientResult = await GetClientOrFailureAsync(clientId, cancellationToken);
        if (!clientResult.IsSuccess)
        {
            return ServiceResult<ClientMeResponse>.Failure(
                clientResult.Status,
                clientResult.Error!.Code,
                clientResult.Error.Message);
        }

        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Phone))
        {
            return ServiceResult<ClientMeResponse>.Failure(
                ResultStatus.Validation,
                "invalid_client_profile",
                "Client full name and phone are required.");
        }

        var client = clientResult.Value!;
        var phone = request.Phone.Trim();
        if (await repository.PhoneExistsAsync(phone, client.Id, cancellationToken))
        {
            return ServiceResult<ClientMeResponse>.Failure(
                ResultStatus.Conflict,
                "phone_conflict",
                "A client with this phone already exists.");
        }

        try
        {
            client.Rename(request.FullName);
            client.ChangePhone(phone);
            await repository.SaveChangesAsync(cancellationToken);
            return ServiceResult<ClientMeResponse>.Success(ToMeResponse(client));
        }
        catch (ArgumentException exception)
        {
            return ServiceResult<ClientMeResponse>.Failure(
                ResultStatus.Validation,
                "invalid_client_profile",
                exception.Message);
        }
    }

    public async Task<ServiceResult<BookingResponse>> CreateBookingAsync(
        Guid clientId,
        CreateClientBookingRequest request,
        CancellationToken cancellationToken)
    {
        var client = await GetClientOrFailureAsync(clientId, cancellationToken);
        if (!client.IsSuccess)
        {
            return ServiceResult<BookingResponse>.Failure(client.Status, client.Error!.Code, client.Error.Message);
        }

        return await bookingService.CreateAsync(
            new CreateBookingRequest(
                client.Value!.FullName,
                client.Value.Phone,
                request.SpecialistId,
                request.ServiceIds,
                request.RequestedDate,
                request.RequestedTime,
                request.Message),
            cancellationToken);
    }

    public async Task<ServiceResult<IReadOnlyCollection<ClientBookingHistoryResponse>>> ListBookingsAsync(
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var client = await GetClientOrFailureAsync(clientId, cancellationToken);
        if (!client.IsSuccess)
        {
            return ServiceResult<IReadOnlyCollection<ClientBookingHistoryResponse>>.Failure(
                client.Status,
                client.Error!.Code,
                client.Error.Message);
        }

        var bookings = await repository.ListBookingsAsync(clientId, cancellationToken);
        return ServiceResult<IReadOnlyCollection<ClientBookingHistoryResponse>>.Success(
            bookings
                .OrderByDescending(booking => booking.CreatedAt)
                .Select(ToHistoryResponse)
                .ToArray());
    }

    public async Task<ServiceResult<IReadOnlyCollection<ClientNotificationResponse>>> ListNotificationsAsync(
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var client = await GetClientOrFailureAsync(clientId, cancellationToken);
        if (!client.IsSuccess)
        {
            return ServiceResult<IReadOnlyCollection<ClientNotificationResponse>>.Failure(
                client.Status,
                client.Error!.Code,
                client.Error.Message);
        }

        var bookings = await repository.ListBookingsAsync(clientId, cancellationToken);
        return ServiceResult<IReadOnlyCollection<ClientNotificationResponse>>.Success(
            bookings
                .Where(booking => !string.IsNullOrWhiteSpace(booking.SpecialistReply) && booking.RepliedAt.HasValue)
                .OrderByDescending(booking => booking.RepliedAt)
                .Select(booking => new ClientNotificationResponse(
                    booking.Id,
                    booking.Specialist?.FullName ?? "Специалист",
                    booking.SpecialistReply!,
                    booking.RepliedAt!.Value))
                .ToArray());
    }

    private async Task<ServiceResult<Client>> GetClientOrFailureAsync(Guid clientId, CancellationToken cancellationToken)
    {
        if (clientId == Guid.Empty)
        {
            return ServiceResult<Client>.Failure(ResultStatus.Validation, "invalid_client_id", "Client id is required.");
        }

        var client = await repository.GetClientByIdAsync(clientId, cancellationToken);
        return client is null
            ? ServiceResult<Client>.Failure(ResultStatus.NotFound, "client_not_found", "Client was not found.")
            : ServiceResult<Client>.Success(client);
    }

    private static ClientBookingHistoryResponse ToHistoryResponse(Booking booking)
    {
        return new ClientBookingHistoryResponse(
            booking.Id,
            booking.SpecialistId,
            booking.Specialist?.FullName ?? "Специалист",
            booking.Specialist?.LocationId,
            booking.Specialist?.Location?.Name,
            booking.Services
                .Select(service => new BookingServiceItemResponse(
                    service.ServiceId,
                    service.ServiceName,
                    service.Price,
                    service.DurationMinutes))
                .ToArray(),
            booking.RequestedDate,
            booking.RequestedTime,
            booking.ConfirmedDate,
            booking.ConfirmedTime,
            booking.TotalPrice,
            booking.TotalDuration,
            booking.Status,
            booking.Message,
            booking.RejectionReason,
            booking.SpecialistReply,
            booking.RepliedAt,
            booking.CreatedAt);
    }

    private static ClientMeResponse ToMeResponse(Client client)
    {
        return new ClientMeResponse(
            client.Id,
            client.FullName,
            client.Email ?? string.Empty,
            client.Phone);
    }
}
