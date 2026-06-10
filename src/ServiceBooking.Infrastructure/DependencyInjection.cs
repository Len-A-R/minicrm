using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Common;
using ServiceBooking.Application.Profile;
using ServiceBooking.Application.Specialists;
using ServiceBooking.Infrastructure.Files;
using ServiceBooking.Infrastructure.Persistence;
using ServiceBooking.Infrastructure.Security;
using ServiceBooking.Infrastructure.Specialists;

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
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAvatarStorage, LocalAvatarStorage>();
        services.AddSingleton<IDateTimeProvider, SystemClock>();

        return services;
    }
}
