using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceBooking.Application.Admin;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Bookings;
using ServiceBooking.Application.Calendar;
using ServiceBooking.Application.Catalog;
using ServiceBooking.Application.Clients;
using ServiceBooking.Application.Common;
using ServiceBooking.Application.Kanban;
using ServiceBooking.Application.Profile;
using ServiceBooking.Application.Reports;
using ServiceBooking.Application.Slots;
using ServiceBooking.Application.SpecialistBookings;
using ServiceBooking.Application.SpecialistClients;
using ServiceBooking.Application.Specialists;
using ServiceBooking.Application.SpecialistServices;
using ServiceBooking.Application.Vacations;
using ServiceBooking.Infrastructure.Admin;
using ServiceBooking.Infrastructure.Bookings;
using ServiceBooking.Infrastructure.Calendar;
using ServiceBooking.Infrastructure.Catalog;
using ServiceBooking.Infrastructure.Clients;
using ServiceBooking.Infrastructure.Files;
using ServiceBooking.Infrastructure.Kanban;
using ServiceBooking.Infrastructure.Persistence;
using ServiceBooking.Infrastructure.Reports;
using ServiceBooking.Infrastructure.Security;
using ServiceBooking.Infrastructure.Slots;
using ServiceBooking.Infrastructure.Specialists;
using ServiceBooking.Infrastructure.SpecialistServices;
using ServiceBooking.Infrastructure.Vacations;

namespace ServiceBooking.Infrastructure;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=service-booking.db";

        services.AddDbContext<ServiceBookingDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<ISpecialistRepository, SpecialistRepository>();
        services.AddScoped<IPublicSpecialistRepository, PublicSpecialistRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IBookingActionRepository, BookingActionRepository>();
        services.AddScoped<ICalendarRepository, CalendarRepository>();
        services.AddScoped<IKanbanRepository, KanbanRepository>();
        services.AddScoped<ISpecialistClientRepository, SpecialistClientRepository>();
        services.AddScoped<IClientAutoCreationRepository, ClientAutoCreationRepository>();
        services.AddScoped<IClientAuthRepository, ClientAuthRepository>();
        services.AddScoped<IClientPortalRepository, ClientPortalRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IServiceCatalogRepository, ServiceCatalogRepository>();
        services.AddScoped<ISpecialistServiceRepository, SpecialistServiceRepository>();
        services.AddScoped<IVacationRepository, VacationRepository>();
        services.AddScoped<ISlotRepository, SlotRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IAdminAuthLookupRepository, AdminRepository>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAvatarStorage, LocalAvatarStorage>();
        services.AddSingleton<IDateTimeProvider, SystemClock>();

        return services;
    }
}
