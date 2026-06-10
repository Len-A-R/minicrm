using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Bookings;
using ServiceBooking.Application.Catalog;
using ServiceBooking.Application.Profile;
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
        services.AddScoped<ISpecialistClientService, SpecialistClientService>();
        services.AddScoped<IClientAutoCreationService, ClientAutoCreationService>();
        services.AddScoped<IPublicSpecialistService, PublicSpecialistService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
        services.AddScoped<ISpecialistServicesService, SpecialistServicesService>();
        services.AddScoped<IVacationService, VacationService>();
        services.AddScoped<ISlotService, SlotService>();

        return services;
    }
}
