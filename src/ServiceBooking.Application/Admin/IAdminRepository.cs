using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;
using DomainService = ServiceBooking.Domain.Entities.Service;

namespace ServiceBooking.Application.Admin;

public interface IAdminRepository
{
    Task<AdminUser?> GetAdminByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<AdminUser?> GetAdminByIdAsync(Guid adminId, CancellationToken cancellationToken);
    Task<bool> AdminEmailExistsAsync(string normalizedEmail, Guid? excludingAdminId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AdminUser>> ListAdminsAsync(CancellationToken cancellationToken);
    Task AddAdminAsync(AdminUser admin, CancellationToken cancellationToken);
    void RemoveAdmin(AdminUser admin);

    Task<IReadOnlyCollection<Specialist>> ListSpecialistsAsync(CancellationToken cancellationToken);
    Task<Specialist?> GetSpecialistAsync(Guid specialistId, CancellationToken cancellationToken);
    void RemoveSpecialist(Specialist specialist);

    Task<IReadOnlyCollection<Booking>> ListBookingsAsync(CancellationToken cancellationToken);
    Task<Booking?> GetBookingAsync(Guid bookingId, CancellationToken cancellationToken);
    void RemoveBooking(Booking booking);

    Task<IReadOnlyCollection<Client>> ListClientsAsync(CancellationToken cancellationToken);
    Task<Client?> GetClientAsync(Guid clientId, CancellationToken cancellationToken);
    void RemoveClient(Client client);

    Task<IReadOnlyCollection<DomainService>> ListServicesAsync(CancellationToken cancellationToken);
    Task<DomainService?> GetServiceAsync(Guid serviceId, CancellationToken cancellationToken);
    Task AddServiceAsync(DomainService service, CancellationToken cancellationToken);
    void RemoveService(DomainService service);
    Task<bool> ServiceHasSpecialistServicesAsync(Guid serviceId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Location>> ListLocationsAsync(CancellationToken cancellationToken);
    Task<Location?> GetLocationAsync(Guid locationId, CancellationToken cancellationToken);
    Task AddLocationAsync(Location location, CancellationToken cancellationToken);
    void RemoveLocation(Location location);
    Task<bool> LocationHasSpecialistsAsync(Guid locationId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SubscriptionPlan>> ListPlansAsync(CancellationToken cancellationToken);
    Task<SubscriptionPlan?> GetPlanAsync(Guid planId, CancellationToken cancellationToken);
    Task<bool> PlanNameExistsAsync(string name, Guid? excludingPlanId, CancellationToken cancellationToken);
    Task AddPlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken);
    void RemovePlan(SubscriptionPlan plan);

    Task<IReadOnlyCollection<SpecialistSubscription>> ListSubscriptionsAsync(CancellationToken cancellationToken);
    Task<SpecialistSubscription?> GetSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken);
    Task<SpecialistSubscription?> GetActiveSubscriptionAsync(Guid specialistId, CancellationToken cancellationToken);
    Task AddSubscriptionAsync(SpecialistSubscription subscription, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PaymentTransaction>> ListPaymentsAsync(CancellationToken cancellationToken);
    Task<PaymentTransaction?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken);
    Task AddPaymentAsync(PaymentTransaction payment, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AuditLog>> ListAuditLogsAsync(CancellationToken cancellationToken);
    Task AddAuditLogAsync(AuditLog log, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SystemSetting>> ListSettingsAsync(CancellationToken cancellationToken);
    Task<SystemSetting?> GetSettingAsync(Guid settingId, CancellationToken cancellationToken);
    Task<SystemSetting?> GetSettingByKeyAsync(string key, CancellationToken cancellationToken);
    Task AddSettingAsync(SystemSetting setting, CancellationToken cancellationToken);
    void RemoveSetting(SystemSetting setting);

    Task<int> CountBookingsAsync(Guid specialistId, DateOnly from, DateOnly to, CancellationToken cancellationToken);
    Task<int> CountSpecialistServicesAsync(Guid specialistId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
