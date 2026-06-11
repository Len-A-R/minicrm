using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Profile;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Tests.Integration;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "ServiceBooking",
                ["Jwt:Audience"] = "ServiceBooking",
                ["Jwt:SigningKey"] = "ServiceBooking-development-signing-key-change-me",
                ["Jwt:AccessTokenMinutes"] = "30",
                ["Jwt:RefreshTokenDays"] = "30",
                ["Database:AutoMigrate"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            _connection.Open();

            services.RemoveAll<DbConnection>();
            services.RemoveAll<DbContextOptions<ServiceBookingDbContext>>();
            services.RemoveAll<IAvatarStorage>();

            services.AddSingleton<DbConnection>(_connection);
            services.AddDbContext<ServiceBookingDbContext>((provider, options) =>
            {
                options.UseSqlite(provider.GetRequiredService<DbConnection>());
            });
            services.AddSingleton<IAvatarStorage, TestAvatarStorage>();

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ServiceBookingDbContext>();
            dbContext.Database.EnsureCreated();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            dbContext.AdminUsers.Add(new AdminUser(
                "Platform Admin",
                "admin@minicrm",
                passwordHasher.Hash("Admin12345")));
            dbContext.SubscriptionPlans.Add(new SubscriptionPlan("Free", 0m, 50, 5, "Starter plan."));
            dbContext.SubscriptionPlans.Add(new SubscriptionPlan("Pro", 1990m, 0, 0, "Unlimited plan."));
            dbContext.SaveChanges();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }

    private sealed class TestAvatarStorage : IAvatarStorage
    {
        public Task<string> SaveAvatarAsync(
            Guid specialistId,
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            return Task.FromResult($"/test-avatars/{fileName}");
        }
    }
}
