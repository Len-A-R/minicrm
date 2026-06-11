using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ServiceBooking.Application.Admin;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Common;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;
using ServiceBooking.Infrastructure.Admin;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Tests.Application;

public sealed class AdminServicesTests
{
    [Fact]
    public async Task AuditLogService_RecordsFiltersAndExportsCsv()
    {
        await using var fixture = await AdminFixture.CreateAsync();
        var service = new AuditLogService(fixture.Repository, fixture.Clock);

        var recorded = await service.RecordAsync(
            Guid.NewGuid(),
            "Admin",
            "POST /api/v1/admin/services",
            "services",
            null,
            "Success",
            "HTTP 201",
            "127.0.0.1",
            CancellationToken.None);
        var list = await service.ListAsync(null, null, null, "services", "services", CancellationToken.None);
        var csv = await service.ExportCsvAsync(null, null, null, null, null, CancellationToken.None);

        Assert.True(recorded.IsSuccess);
        Assert.Single(list.Value!);
        Assert.Contains("POST /api/v1/admin/services", csv.Value);
    }

    [Fact]
    public async Task SubscriptionQuotaService_ReturnsConflictWhenPlanLimitsAreReached()
    {
        await using var fixture = await AdminFixture.CreateAsync();
        var specialist = new Specialist("Jane Doe", "jane@example.com", "+15550101010", "password-hash");
        var plan = new SubscriptionPlan("Starter", 0m, 1, 1);
        var service = new ServiceBooking.Domain.Entities.Service("Consultation");
        fixture.DbContext.Specialists.Add(specialist);
        fixture.DbContext.SubscriptionPlans.Add(plan);
        fixture.DbContext.Services.Add(service);
        await fixture.DbContext.SaveChangesAsync();

        fixture.DbContext.SpecialistSubscriptions.Add(new SpecialistSubscription(
            specialist.Id,
            plan.Id,
            fixture.Clock.UtcNow.AddDays(-1),
            fixture.Clock.UtcNow.AddDays(30)));
        fixture.DbContext.SpecialistServices.Add(new SpecialistService(specialist.Id, service.Id, 100m, 60));
        fixture.DbContext.Bookings.Add(new Booking(
            "Alice Brown",
            "+15550909090",
            specialist.Id,
            new DateOnly(2026, 6, 15),
            new TimeOnly(10, 0),
            [new BookingService(service.Id, service.Name, 100m, 60)]));
        await fixture.DbContext.SaveChangesAsync();

        var quota = new SubscriptionQuotaService(fixture.Repository, fixture.Clock);
        var bookingQuota = await quota.CheckBookingQuotaAsync(specialist.Id, CancellationToken.None);
        var serviceQuota = await quota.CheckServiceQuotaAsync(specialist.Id, CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, bookingQuota.Status);
        Assert.Equal("booking_quota_exceeded", bookingQuota.Error?.Code);
        Assert.Equal(ResultStatus.Conflict, serviceQuota.Status);
        Assert.Equal("service_quota_exceeded", serviceQuota.Error?.Code);
    }

    [Fact]
    public async Task PaymentService_WebhookMarksPaymentAndRenewsSubscription()
    {
        await using var fixture = await AdminFixture.CreateAsync();
        var specialist = new Specialist("Jane Doe", "jane@example.com", "+15550101010", "password-hash");
        var plan = new SubscriptionPlan("Pro", 1990m, 0, 0);
        fixture.DbContext.Specialists.Add(specialist);
        fixture.DbContext.SubscriptionPlans.Add(plan);
        await fixture.DbContext.SaveChangesAsync();
        var subscription = new SpecialistSubscription(
            specialist.Id,
            plan.Id,
            fixture.Clock.UtcNow.AddDays(-10),
            fixture.Clock.UtcNow.AddDays(5));
        fixture.DbContext.SpecialistSubscriptions.Add(subscription);
        await fixture.DbContext.SaveChangesAsync();

        var payments = new PaymentService(fixture.Repository, fixture.Clock);
        var created = await payments.CreateAsync(
            new PaymentCreateRequest(specialist.Id, subscription.Id, 1990m, "rub"),
            CancellationToken.None);
        var webhook = await payments.ProcessWebhookAsync(
            new PaymentWebhookRequest(created.Value!.Id, PaymentStatus.Succeeded, "mock-1", null),
            CancellationToken.None);

        var updatedSubscription = await fixture.DbContext.SpecialistSubscriptions.SingleAsync(item => item.Id == subscription.Id);
        Assert.True(webhook.IsSuccess);
        Assert.Equal(PaymentStatus.Succeeded, webhook.Value!.Status);
        Assert.True(updatedSubscription.ExpiresAt > fixture.Clock.UtcNow.AddDays(20));
    }

    [Fact]
    public async Task AdminActionService_BlocksSpecialistAssignsPlanAndUpsertsSetting()
    {
        await using var fixture = await AdminFixture.CreateAsync();
        var specialist = new Specialist("Jane Doe", "jane@example.com", "+15550101010", "password-hash");
        var plan = new SubscriptionPlan("Pro", 1990m, 0, 0);
        fixture.DbContext.Specialists.Add(specialist);
        fixture.DbContext.SubscriptionPlans.Add(plan);
        await fixture.DbContext.SaveChangesAsync();

        var service = new AdminActionService(fixture.Repository, new FakePasswordHasher(), fixture.Clock);
        var blocked = await service.BlockSpecialistAsync(specialist.Id, new BlockSpecialistRequest("Policy"), CancellationToken.None);
        var planned = await service.ChangeSpecialistPlanAsync(
            specialist.Id,
            new ChangeSpecialistPlanRequest(plan.Id, new DateOnly(2026, 7, 20)),
            CancellationToken.None);
        var setting = await service.UpsertSettingAsync(
            new UpsertSystemSettingRequest("platform.support_email", "support@example.com", "Support"),
            CancellationToken.None);

        Assert.True(blocked.Value!.IsBlocked);
        Assert.Equal("Pro", planned.Value!.SubscriptionPlanName);
        Assert.Equal("platform.support_email", setting.Value!.Key);
    }

    [Fact]
    public async Task AdminActionService_CoversCrudFiltersAndDeletionFlows()
    {
        await using var fixture = await AdminFixture.CreateAsync();
        var service = new AdminActionService(fixture.Repository, new FakePasswordHasher(), fixture.Clock);
        var specialist = new Specialist("Jane Doe", "jane@example.com", "+15550101010", "password-hash");
        var client = new Client("Alice Brown", "+15550909090");
        fixture.DbContext.Specialists.Add(specialist);
        fixture.DbContext.Clients.Add(client);
        await fixture.DbContext.SaveChangesAsync();
        var booking = new Booking(
            "Alice Brown",
            "+15550909090",
            specialist.Id,
            new DateOnly(2026, 6, 15),
            new TimeOnly(10, 0),
            [],
            "Message only",
            client.Id);
        fixture.DbContext.Bookings.Add(booking);
        await fixture.DbContext.SaveChangesAsync();

        var createdService = await service.CreateServiceAsync(new UpsertAdminServiceRequest("Admin Service", "One"), CancellationToken.None);
        var updatedService = await service.UpdateServiceAsync(
            createdService.Value!.Id,
            new UpsertAdminServiceRequest("Admin Service Updated", "Two"),
            CancellationToken.None);
        var deletedService = await service.DeleteServiceAsync(createdService.Value.Id, CancellationToken.None);

        var createdLocation = await service.CreateLocationAsync(
            new UpsertAdminLocationRequest("Admin Location", "10 Main", "One"),
            CancellationToken.None);
        var updatedLocation = await service.UpdateLocationAsync(
            createdLocation.Value!.Id,
            new UpsertAdminLocationRequest("Admin Location Updated", "11 Main", "Two"),
            CancellationToken.None);
        var deletedLocation = await service.DeleteLocationAsync(createdLocation.Value.Id, CancellationToken.None);

        var createdPlan = await service.CreatePlanAsync(
            new UpsertSubscriptionPlanRequest("Temporary", "Temp", 100m, 3, 2, true),
            CancellationToken.None);
        var updatedPlan = await service.UpdatePlanAsync(
            createdPlan.Value!.Id,
            new UpsertSubscriptionPlanRequest("Temporary Updated", "Temp 2", 200m, 4, 3, true),
            CancellationToken.None);
        var deleteOnlyPlan = await service.CreatePlanAsync(
            new UpsertSubscriptionPlanRequest("Delete Only", null, 10m, 1, 1, true),
            CancellationToken.None);
        var deletedPlan = await service.DeletePlanAsync(deleteOnlyPlan.Value!.Id, CancellationToken.None);

        var createdAdmin = await service.UpsertAdminAsync(
            null,
            new UpsertAdminUserRequest("Admin One", "admin1@example.com", "Password1", true),
            CancellationToken.None);
        var createdSecondAdmin = await service.UpsertAdminAsync(
            null,
            new UpsertAdminUserRequest("Admin Two", "admin2@example.com", "Password1", true),
            CancellationToken.None);
        var updatedAdmin = await service.UpsertAdminAsync(
            createdAdmin.Value!.Id,
            new UpsertAdminUserRequest("Admin One Updated", "admin1-updated@example.com", null, true),
            CancellationToken.None);
        var deletedAdmin = await service.DeleteAdminAsync(createdSecondAdmin.Value!.Id, CancellationToken.None);

        var assignedPlan = await service.ChangeSpecialistPlanAsync(
            specialist.Id,
            new ChangeSpecialistPlanRequest(updatedPlan.Value!.Id, new DateOnly(2026, 7, 31)),
            CancellationToken.None);
        var subscription = (await service.ListSubscriptionsAsync(null, specialist.Id, CancellationToken.None)).Value!.Single();
        var suspended = await service.ChangeSubscriptionStatusAsync(
            subscription.Id,
            new AdminSubscriptionStatusRequest(SubscriptionStatus.Suspended),
            CancellationToken.None);
        var renewed = await service.RenewSubscriptionAsync(
            subscription.Id,
            new RenewSubscriptionRequest(new DateOnly(2026, 8, 31)),
            CancellationToken.None);

        fixture.DbContext.PaymentTransactions.Add(new PaymentTransaction(specialist.Id, subscription.Id, 200m, "RUB", "mock"));
        await fixture.DbContext.SaveChangesAsync();
        var payments = await service.ListPaymentsAsync(PaymentStatus.Pending, specialist.Id, null, null, CancellationToken.None);
        var bookings = await service.ListBookingsAsync(null, specialist.Id, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), "Alice", CancellationToken.None);
        var completed = await service.ChangeBookingStatusAsync(
            booking.Id,
            new AdminBookingStatusRequest(BookingStatus.Completed, 200m),
            CancellationToken.None);
        var deletedBooking = await service.DeleteBookingAsync(booking.Id, CancellationToken.None);
        var deletedClient = await service.DeleteClientAsync(client.Id, CancellationToken.None);
        var deletedSpecialist = await service.DeleteSpecialistAsync(specialist.Id, CancellationToken.None);

        Assert.True(updatedService.IsSuccess);
        Assert.True(deletedService.Value);
        Assert.True(updatedLocation.IsSuccess);
        Assert.True(deletedLocation.Value);
        Assert.True(deletedPlan.Value);
        Assert.True(updatedAdmin.IsSuccess);
        Assert.True(deletedAdmin.Value);
        Assert.Equal("Temporary Updated", assignedPlan.Value!.SubscriptionPlanName);
        Assert.Equal(SubscriptionStatus.Suspended, suspended.Value!.Status);
        Assert.Equal(SubscriptionStatus.Active, renewed.Value!.Status);
        Assert.Single(payments.Value!);
        Assert.Single(bookings.Value!);
        Assert.Equal(BookingStatus.Completed, completed.Value!.Status);
        Assert.True(deletedBooking.Value);
        Assert.True(deletedClient.Value);
        Assert.True(deletedSpecialist.Value);
    }

    [Fact]
    public async Task AdminAuthService_ReturnsMeAndRejectsInvalidCredentials()
    {
        await using var fixture = await AdminFixture.CreateAsync();
        var passwordHasher = new FakePasswordHasher();
        var admin = new AdminUser("Admin One", "admin@minicrm", passwordHasher.Hash("Password1"));
        fixture.DbContext.AdminUsers.Add(admin);
        await fixture.DbContext.SaveChangesAsync();

        var auth = new AdminAuthService(fixture.Repository, passwordHasher, new FakeTokenService(), fixture.Clock);
        var login = await auth.LoginAsync(new AdminLoginRequest("admin@minicrm", "Password1"), CancellationToken.None);
        var me = await auth.GetMeAsync(admin.Id, CancellationToken.None);
        var invalid = await auth.LoginAsync(new AdminLoginRequest("admin@minicrm", "wrong"), CancellationToken.None);

        Assert.True(login.IsSuccess);
        Assert.True(me.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, invalid.Status);
    }

    private sealed class AdminFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private AdminFixture(SqliteConnection connection, ServiceBookingDbContext dbContext)
        {
            _connection = connection;
            DbContext = dbContext;
            Repository = new AdminRepository(dbContext);
            Clock = new FakeDateTimeProvider(new DateTimeOffset(2026, 6, 11, 12, 0, 0, TimeSpan.Zero));
        }

        public ServiceBookingDbContext DbContext { get; }
        public AdminRepository Repository { get; }
        public FakeDateTimeProvider Clock { get; }

        public static async Task<AdminFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ServiceBookingDbContext>()
                .UseSqlite(connection)
                .Options;
            var dbContext = new ServiceBookingDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();
            return new AdminFixture(connection, dbContext);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class FakeDateTimeProvider(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string value) => $"hash:{value}";

        public bool Verify(string value, string hash) => hash == Hash(value);
    }

    private sealed class FakeTokenService : ITokenService
    {
        public AccessTokenResult CreateAccessToken(Specialist specialist, DateTimeOffset utcNow)
        {
            return new AccessTokenResult("access", utcNow.AddMinutes(30));
        }

        public AccessTokenResult CreateClientAccessToken(Client client, DateTimeOffset utcNow)
        {
            return new AccessTokenResult("client-access", utcNow.AddMinutes(30));
        }

        public AccessTokenResult CreateAdminAccessToken(AdminUser admin, DateTimeOffset utcNow)
        {
            return new AccessTokenResult("admin-access", utcNow.AddMinutes(30));
        }

        public RefreshTokenResult CreateRefreshToken(DateTimeOffset utcNow)
        {
            return new RefreshTokenResult("refresh", utcNow.AddDays(30));
        }
    }
}
