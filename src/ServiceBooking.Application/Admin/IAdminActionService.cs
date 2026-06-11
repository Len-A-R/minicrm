using ServiceBooking.Application.Common;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Application.Admin;

public interface IAdminActionService
{
    Task<ServiceResult<IReadOnlyCollection<AdminSpecialistResponse>>> ListSpecialistsAsync(string? search, bool? blocked, CancellationToken cancellationToken);
    Task<ServiceResult<AdminSpecialistResponse>> GetSpecialistAsync(Guid specialistId, CancellationToken cancellationToken);
    Task<ServiceResult<AdminSpecialistResponse>> BlockSpecialistAsync(Guid specialistId, BlockSpecialistRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<AdminSpecialistResponse>> UnblockSpecialistAsync(Guid specialistId, CancellationToken cancellationToken);
    Task<ServiceResult<AdminSpecialistResponse>> ChangeSpecialistPlanAsync(Guid specialistId, ChangeSpecialistPlanRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteSpecialistAsync(Guid specialistId, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<AdminBookingResponse>>> ListBookingsAsync(BookingStatus? status, Guid? specialistId, DateOnly? from, DateOnly? to, string? search, CancellationToken cancellationToken);
    Task<ServiceResult<AdminBookingResponse>> GetBookingAsync(Guid bookingId, CancellationToken cancellationToken);
    Task<ServiceResult<AdminBookingResponse>> ChangeBookingStatusAsync(Guid bookingId, AdminBookingStatusRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteBookingAsync(Guid bookingId, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<AdminClientResponse>>> ListClientsAsync(string? search, ClientStatus? status, CancellationToken cancellationToken);
    Task<ServiceResult<AdminClientResponse>> GetClientAsync(Guid clientId, CancellationToken cancellationToken);
    Task<ServiceResult<AdminClientResponse>> UpdateClientAsync(Guid clientId, AdminClientUpdateRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteClientAsync(Guid clientId, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<AdminServiceResponse>>> ListServicesAsync(CancellationToken cancellationToken);
    Task<ServiceResult<AdminServiceResponse>> GetServiceAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<ServiceResult<AdminServiceResponse>> CreateServiceAsync(UpsertAdminServiceRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<AdminServiceResponse>> UpdateServiceAsync(Guid serviceId, UpsertAdminServiceRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteServiceAsync(Guid serviceId, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<AdminLocationResponse>>> ListLocationsAsync(CancellationToken cancellationToken);
    Task<ServiceResult<AdminLocationResponse>> GetLocationAsync(Guid locationId, CancellationToken cancellationToken);
    Task<ServiceResult<AdminLocationResponse>> CreateLocationAsync(UpsertAdminLocationRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<AdminLocationResponse>> UpdateLocationAsync(Guid locationId, UpsertAdminLocationRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteLocationAsync(Guid locationId, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<AdminPaymentResponse>>> ListPaymentsAsync(PaymentStatus? status, Guid? specialistId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
    Task<ServiceResult<AdminPaymentResponse>> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken);
    Task<ServiceResult<PlatformFinanceSummaryResponse>> GetFinanceSummaryAsync(CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<AdminSubscriptionResponse>>> ListSubscriptionsAsync(SubscriptionStatus? status, Guid? specialistId, CancellationToken cancellationToken);
    Task<ServiceResult<AdminSubscriptionResponse>> ChangeSubscriptionStatusAsync(Guid subscriptionId, AdminSubscriptionStatusRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<AdminSubscriptionResponse>> RenewSubscriptionAsync(Guid subscriptionId, RenewSubscriptionRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<AdminSettingResponse>>> ListSettingsAsync(CancellationToken cancellationToken);
    Task<ServiceResult<AdminSettingResponse>> UpsertSettingAsync(UpsertSystemSettingRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteSettingAsync(Guid settingId, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<AdminUserResponse>>> ListAdminsAsync(CancellationToken cancellationToken);
    Task<ServiceResult<AdminUserResponse>> UpsertAdminAsync(Guid? adminId, UpsertAdminUserRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteAdminAsync(Guid adminId, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyCollection<SubscriptionPlanResponse>>> ListPlansAsync(bool activeOnly, CancellationToken cancellationToken);
    Task<ServiceResult<SubscriptionPlanResponse>> GetPlanAsync(Guid planId, CancellationToken cancellationToken);
    Task<ServiceResult<SubscriptionPlanResponse>> CreatePlanAsync(UpsertSubscriptionPlanRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<SubscriptionPlanResponse>> UpdatePlanAsync(Guid planId, UpsertSubscriptionPlanRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeletePlanAsync(Guid planId, CancellationToken cancellationToken);
}
