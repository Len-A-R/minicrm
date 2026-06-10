using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceBooking.Application.Profile;
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
                ["Jwt:RefreshTokenDays"] = "30"
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
