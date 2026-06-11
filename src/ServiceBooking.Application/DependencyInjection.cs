using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ServiceBooking.Application.Admin;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Bookings;
using ServiceBooking.Application.Calendar;
using ServiceBooking.Application.Catalog;
using ServiceBooking.Application.Clients;
using ServiceBooking.Application.Kanban;
using ServiceBooking.Application.Profile;
using ServiceBooking.Application.Reports;
using ServiceBooking.Application.Slots;
using ServiceBooking.Application.SpecialistBookings;
using ServiceBooking.Application.SpecialistClients;
using ServiceBooking.Application.SpecialistServices;
using ServiceBooking.Application.Specialists;
using ServiceBooking.Application.Vacations;

namespace ServiceBooking.Application;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IBookingService, Bookings.BookingService>();
        services.AddScoped<IBookingActionService, BookingActionService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IKanbanService, KanbanService>();
        services.AddScoped<ISpecialistClientService, SpecialistClientService>();
        services.AddScoped<IClientAutoCreationService, ClientAutoCreationService>();
        services.AddScoped<IClientPortalService, ClientPortalService>();
        services.AddScoped<IPublicSpecialistService, PublicSpecialistService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
        services.AddScoped<ISpecialistServicesService, SpecialistServicesService>();
        services.AddScoped<IVacationService, VacationService>();
        services.AddScoped<ISlotService, SlotService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAdminAuthService, AdminAuthService>();
        services.AddScoped<IAdminActionService, AdminActionService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ISubscriptionQuotaService, SubscriptionQuotaService>();
        services.AddScoped<IPaymentService, PaymentService>();

        return services;
    }
}
