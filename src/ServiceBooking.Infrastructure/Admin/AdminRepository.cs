using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.Admin;
using ServiceBooking.Application.Auth;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;
using ServiceBooking.Infrastructure.Persistence;
using DomainService = ServiceBooking.Domain.Entities.Service;

namespace ServiceBooking.Infrastructure.Admin;

public sealed class AdminRepository(ServiceBookingDbContext dbContext) : IAdminRepository, IAdminAuthLookupRepository
{
    public Task<AdminUser?> GetAdminByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return dbContext.AdminUsers.SingleOrDefaultAsync(admin => admin.Email == normalizedEmail, cancellationToken);
    }

    public Task<AdminUser?> GetAdminByIdAsync(Guid adminId, CancellationToken cancellationToken)
    {
        return dbContext.AdminUsers.SingleOrDefaultAsync(admin => admin.Id == adminId, cancellationToken);
    }

    public Task<bool> AdminEmailExistsAsync(string normalizedEmail, Guid? excludingAdminId, CancellationToken cancellationToken)
    {
        return dbContext.AdminUsers.AnyAsync(
            admin => admin.Email == normalizedEmail && (!excludingAdminId.HasValue || admin.Id != excludingAdminId.Value),
            cancellationToken);
    }

    public Task<bool> AdminEmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return dbContext.AdminUsers.AnyAsync(admin => admin.Email == normalizedEmail, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AdminUser>> ListAdminsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.AdminUsers.ToArrayAsync(cancellationToken);
    }

    public async Task AddAdminAsync(AdminUser admin, CancellationToken cancellationToken)
    {
        await dbContext.AdminUsers.AddAsync(admin, cancellationToken);
    }

    public void RemoveAdmin(AdminUser admin) => dbContext.AdminUsers.Remove(admin);

    public async Task<IReadOnlyCollection<Specialist>> ListSpecialistsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Specialists
            .Include(specialist => specialist.Location)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Specialist?> GetSpecialistAsync(Guid specialistId, CancellationToken cancellationToken)
    {
        return dbContext.Specialists
            .Include(specialist => specialist.Location)
            .SingleOrDefaultAsync(specialist => specialist.Id == specialistId, cancellationToken);
    }

    public void RemoveSpecialist(Specialist specialist) => dbContext.Specialists.Remove(specialist);

    public async Task<IReadOnlyCollection<Booking>> ListBookingsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Bookings
            .Include(booking => booking.Specialist)
            .Include(booking => booking.Services)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Booking?> GetBookingAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        return dbContext.Bookings
            .Include(booking => booking.Specialist)
            .Include(booking => booking.Services)
            .SingleOrDefaultAsync(booking => booking.Id == bookingId, cancellationToken);
    }

    public void RemoveBooking(Booking booking) => dbContext.Bookings.Remove(booking);

    public async Task<IReadOnlyCollection<Client>> ListClientsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Clients
            .Include(client => client.Bookings)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Client?> GetClientAsync(Guid clientId, CancellationToken cancellationToken)
    {
        return dbContext.Clients
            .Include(client => client.Bookings)
            .SingleOrDefaultAsync(client => client.Id == clientId, cancellationToken);
    }

    public void RemoveClient(Client client) => dbContext.Clients.Remove(client);

    public async Task<IReadOnlyCollection<DomainService>> ListServicesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Services.ToArrayAsync(cancellationToken);
    }

    public Task<DomainService?> GetServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return dbContext.Services.SingleOrDefaultAsync(service => service.Id == serviceId, cancellationToken);
    }

    public async Task AddServiceAsync(DomainService service, CancellationToken cancellationToken)
    {
        await dbContext.Services.AddAsync(service, cancellationToken);
    }

    public void RemoveService(DomainService service) => dbContext.Services.Remove(service);

    public Task<bool> ServiceHasSpecialistServicesAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return dbContext.SpecialistServices.AnyAsync(service => service.ServiceId == serviceId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Location>> ListLocationsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Locations.ToArrayAsync(cancellationToken);
    }

    public Task<Location?> GetLocationAsync(Guid locationId, CancellationToken cancellationToken)
    {
        return dbContext.Locations.SingleOrDefaultAsync(location => location.Id == locationId, cancellationToken);
    }

    public async Task AddLocationAsync(Location location, CancellationToken cancellationToken)
    {
        await dbContext.Locations.AddAsync(location, cancellationToken);
    }

    public void RemoveLocation(Location location) => dbContext.Locations.Remove(location);

    public Task<bool> LocationHasSpecialistsAsync(Guid locationId, CancellationToken cancellationToken)
    {
        return dbContext.Specialists.AnyAsync(specialist => specialist.LocationId == locationId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<SubscriptionPlan>> ListPlansAsync(CancellationToken cancellationToken)
    {
        return await dbContext.SubscriptionPlans.ToArrayAsync(cancellationToken);
    }

    public Task<SubscriptionPlan?> GetPlanAsync(Guid planId, CancellationToken cancellationToken)
    {
        return dbContext.SubscriptionPlans.SingleOrDefaultAsync(plan => plan.Id == planId, cancellationToken);
    }

    public Task<bool> PlanNameExistsAsync(string name, Guid? excludingPlanId, CancellationToken cancellationToken)
    {
        return dbContext.SubscriptionPlans.AnyAsync(
            plan => plan.Name == name && (!excludingPlanId.HasValue || plan.Id != excludingPlanId.Value),
            cancellationToken);
    }

    public async Task AddPlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        await dbContext.SubscriptionPlans.AddAsync(plan, cancellationToken);
    }

    public void RemovePlan(SubscriptionPlan plan) => dbContext.SubscriptionPlans.Remove(plan);

    public async Task<IReadOnlyCollection<SpecialistSubscription>> ListSubscriptionsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.SpecialistSubscriptions
            .Include(subscription => subscription.Specialist)
            .Include(subscription => subscription.Plan)
            .ToArrayAsync(cancellationToken);
    }

    public Task<SpecialistSubscription?> GetSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        return dbContext.SpecialistSubscriptions
            .Include(subscription => subscription.Specialist)
            .Include(subscription => subscription.Plan)
            .SingleOrDefaultAsync(subscription => subscription.Id == subscriptionId, cancellationToken);
    }

    public async Task<SpecialistSubscription?> GetActiveSubscriptionAsync(Guid specialistId, CancellationToken cancellationToken)
    {
        var subscriptions = await dbContext.SpecialistSubscriptions
            .Include(subscription => subscription.Specialist)
            .Include(subscription => subscription.Plan)
            .Where(subscription => subscription.SpecialistId == specialistId
                && (subscription.Status == SubscriptionStatus.Active || subscription.Status == SubscriptionStatus.Trial))
            .ToArrayAsync(cancellationToken);

        return subscriptions
            .OrderByDescending(subscription => subscription.ExpiresAt)
            .FirstOrDefault();
    }

    public async Task AddSubscriptionAsync(SpecialistSubscription subscription, CancellationToken cancellationToken)
    {
        await dbContext.SpecialistSubscriptions.AddAsync(subscription, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PaymentTransaction>> ListPaymentsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.PaymentTransactions
            .Include(payment => payment.Specialist)
            .Include(payment => payment.Subscription)
            .ToArrayAsync(cancellationToken);
    }

    public Task<PaymentTransaction?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        return dbContext.PaymentTransactions
            .Include(payment => payment.Specialist)
            .Include(payment => payment.Subscription)
            .SingleOrDefaultAsync(payment => payment.Id == paymentId, cancellationToken);
    }

    public async Task AddPaymentAsync(PaymentTransaction payment, CancellationToken cancellationToken)
    {
        await dbContext.PaymentTransactions.AddAsync(payment, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditLog>> ListAuditLogsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.AuditLogs.ToArrayAsync(cancellationToken);
    }

    public async Task AddAuditLogAsync(AuditLog log, CancellationToken cancellationToken)
    {
        await dbContext.AuditLogs.AddAsync(log, cancellationToken);
    }

    public async Task<IReadOnlyCollection<SystemSetting>> ListSettingsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.SystemSettings.ToArrayAsync(cancellationToken);
    }

    public Task<SystemSetting?> GetSettingAsync(Guid settingId, CancellationToken cancellationToken)
    {
        return dbContext.SystemSettings.SingleOrDefaultAsync(setting => setting.Id == settingId, cancellationToken);
    }

    public Task<SystemSetting?> GetSettingByKeyAsync(string key, CancellationToken cancellationToken)
    {
        return dbContext.SystemSettings.SingleOrDefaultAsync(setting => setting.Key == key, cancellationToken);
    }

    public async Task AddSettingAsync(SystemSetting setting, CancellationToken cancellationToken)
    {
        await dbContext.SystemSettings.AddAsync(setting, cancellationToken);
    }

    public void RemoveSetting(SystemSetting setting) => dbContext.SystemSettings.Remove(setting);

    public Task<int> CountBookingsAsync(Guid specialistId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        return dbContext.Bookings.CountAsync(
            booking => booking.SpecialistId == specialistId
                && booking.RequestedDate >= from
                && booking.RequestedDate <= to,
            cancellationToken);
    }

    public Task<int> CountSpecialistServicesAsync(Guid specialistId, CancellationToken cancellationToken)
    {
        return dbContext.SpecialistServices.CountAsync(service => service.SpecialistId == specialistId, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
