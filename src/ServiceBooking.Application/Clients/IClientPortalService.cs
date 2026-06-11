using ServiceBooking.Application.Bookings;
using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Clients;

public interface IClientPortalService
{
    Task<ServiceResult<ClientMeResponse>> GetMeAsync(Guid clientId, CancellationToken cancellationToken);

    Task<ServiceResult<ClientMeResponse>> UpdateMeAsync(
        Guid clientId,
        UpdateClientProfileRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<BookingResponse>> CreateBookingAsync(
        Guid clientId,
        CreateClientBookingRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<ClientBookingHistoryResponse>>> ListBookingsAsync(
        Guid clientId,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<ClientNotificationResponse>>> ListNotificationsAsync(
        Guid clientId,
        CancellationToken cancellationToken);
}
