using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ServiceBooking.Application.Admin;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Bookings;
using ServiceBooking.Application.Catalog;
using ServiceBooking.Application.SpecialistBookings;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Tests.Integration;

public sealed class Stage8AdministrationEndpointTests
{
    [Fact]
    public async Task AdminAuthAndSpecialistManagementFlow_WorksAndRequiresAdminRole()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var specialistAuth = await RegisterSpecialistAsync(client);

        var unauthorized = await client.GetAsync("/api/v1/admin/specialists");
        var admin = await LoginAdminAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var specialists = await client.GetFromJsonAsync<IReadOnlyCollection<AdminSpecialistResponse>>("/api/v1/admin/specialists");
        var specialist = specialists!.Single(item => item.Id == specialistAuth.SpecialistId);
        var plans = await client.GetFromJsonAsync<IReadOnlyCollection<SubscriptionPlanResponse>>("/api/v1/subscription-plans");
        var pro = plans!.Single(plan => plan.Name == "Pro");

        var planResponse = await client.PutAsJsonAsync(
            $"/api/v1/admin/specialists/{specialist.Id}/plan",
            new ChangeSpecialistPlanRequest(pro.Id, new DateOnly(2026, 7, 31)));
        planResponse.EnsureSuccessStatusCode();
        var planned = await planResponse.Content.ReadFromJsonAsync<AdminSpecialistResponse>();

        var blockResponse = await client.PutAsJsonAsync(
            $"/api/v1/admin/specialists/{specialist.Id}/block",
            new BlockSpecialistRequest("Policy"));
        blockResponse.EnsureSuccessStatusCode();
        var blocked = await blockResponse.Content.ReadFromJsonAsync<AdminSpecialistResponse>();

        client.DefaultRequestHeaders.Authorization = null;
        var blockedLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(
            specialistAuth.Email,
            "Password1"));

        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal("Pro", planned!.SubscriptionPlanName);
        Assert.True(blocked!.IsBlocked);
        Assert.Equal(HttpStatusCode.Unauthorized, blockedLogin.StatusCode);
    }

    [Fact]
    public async Task UnifiedLogin_DetectsDefaultAdminRole()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(
            "admin@minicrm",
            "Admin12345"));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var specialists = await client.GetAsync("/api/v1/admin/specialists");
        var oldAdmin = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(
            "admin@servicebooking.local",
            "Admin12345"));

        Assert.Equal("Admin", auth.Role);
        Assert.Equal(HttpStatusCode.OK, specialists.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldAdmin.StatusCode);
    }

    [Fact]
    public async Task AdminGlobalBookingsClientsCatalogsAndAuditFlow_Works()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var setup = await CreatePublicBookingSetupAsync(client);
        var booking = await CreateBookingAsync(client, setup.SpecialistId, setup.ServiceId);
        var admin = await LoginAdminAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var bookings = await client.GetFromJsonAsync<IReadOnlyCollection<AdminBookingResponse>>("/api/v1/admin/bookings");
        var statusResponse = await client.PutAsJsonAsync(
            $"/api/v1/admin/bookings/{booking.Id}/status",
            new AdminBookingStatusRequest(BookingStatus.Completed, 150m));
        statusResponse.EnsureSuccessStatusCode();

        var clients = await client.GetFromJsonAsync<IReadOnlyCollection<AdminClientResponse>>("/api/v1/admin/clients");
        var clientRow = clients!.Single(item => item.Phone == "+15550909090");
        var clientUpdate = await client.PutAsJsonAsync(
            $"/api/v1/admin/clients/{clientRow.Id}",
            new AdminClientUpdateRequest(clientRow.FullName, clientRow.Phone, ClientStatus.Vip, "admin-tag"));
        clientUpdate.EnsureSuccessStatusCode();

        var serviceResponse = await client.PostAsJsonAsync(
            "/api/v1/admin/services",
            new UpsertAdminServiceRequest($"Admin Service {Guid.NewGuid():N}", "Created by admin"));
        serviceResponse.EnsureSuccessStatusCode();
        var locationResponse = await client.PostAsJsonAsync(
            "/api/v1/admin/locations",
            new UpsertAdminLocationRequest($"Admin Location {Guid.NewGuid():N}", "Admin Street", "Created by admin"));
        locationResponse.EnsureSuccessStatusCode();

        var settingResponse = await client.PutAsJsonAsync(
            "/api/v1/admin/settings",
            new UpsertSystemSettingRequest("stage8.test", "enabled", "Stage 8 integration test"));
        settingResponse.EnsureSuccessStatusCode();

        var auditLogs = await client.GetFromJsonAsync<IReadOnlyCollection<AdminAuditLogResponse>>("/api/v1/admin/audit-logs");

        Assert.Contains(bookings!, item => item.Id == booking.Id);
        Assert.Contains(auditLogs!, item => item.Action.Contains("/api/v1/admin/services", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(auditLogs!, item => item.Action.Contains("/api/v1/admin/settings", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AdminPaymentsSubscriptionsAndFrontend_AreAvailable()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var specialistAuth = await RegisterSpecialistAsync(client);
        var admin = await LoginAdminAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
        var plans = await client.GetFromJsonAsync<IReadOnlyCollection<SubscriptionPlanResponse>>("/api/v1/subscription-plans");
        var pro = plans!.Single(plan => plan.Name == "Pro");
        await client.PutAsJsonAsync(
            $"/api/v1/admin/specialists/{specialistAuth.SpecialistId}/plan",
            new ChangeSpecialistPlanRequest(pro.Id, new DateOnly(2026, 7, 31)));
        var subscriptions = await client.GetFromJsonAsync<IReadOnlyCollection<AdminSubscriptionResponse>>("/api/v1/admin/subscriptions");
        var subscription = subscriptions!.Single(item => item.SpecialistId == specialistAuth.SpecialistId);

        var paymentResponse = await client.PostAsJsonAsync(
            "/api/v1/admin/payments",
            new PaymentCreateRequest(specialistAuth.SpecialistId, subscription.Id, 1990m, "RUB"));
        paymentResponse.EnsureSuccessStatusCode();
        var payment = await paymentResponse.Content.ReadFromJsonAsync<AdminPaymentResponse>();

        client.DefaultRequestHeaders.Authorization = null;
        var webhook = await client.PostAsJsonAsync(
            "/api/v1/admin/payments/webhook",
            new PaymentWebhookRequest(payment!.Id, PaymentStatus.Succeeded, "mock-stage8", null));
        webhook.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
        var summary = await client.GetFromJsonAsync<PlatformFinanceSummaryResponse>("/api/v1/admin/payments/summary");
        var adminHtml = await client.GetStringAsync("/admin.html");
        var adminJs = await client.GetStringAsync("/admin.js");
        var styles = await client.GetStringAsync("/styles.css");

        Assert.True(summary!.TotalRevenue >= 1990m);
        Assert.Contains("Администрирование", adminHtml);
        Assert.Contains("id=\"admin-sidebar\" hidden", adminHtml);
        Assert.Contains("id=\"admin-nav\"", adminHtml);
        Assert.DoesNotContain("dashboard-topbar", adminHtml);
        Assert.DoesNotContain("admin-refresh-button", adminHtml);
        Assert.DoesNotContain("admin-logout-button", adminHtml);
        Assert.Contains("adminMenuItems", adminJs);
        Assert.Contains("route: \"specialists\"", adminJs);
        Assert.Contains("route: \"audit\"", adminJs);
        Assert.Contains("startAdminAutoRefresh", adminJs);
        Assert.Contains("adminNavIcon", adminJs);
        Assert.Contains("data-admin-logout", adminJs);
        Assert.Contains("location.href = \"/login\"", adminJs);
        Assert.Contains("/api/v1/admin/specialists", adminJs);
        Assert.Contains("/api/v1/admin/audit-logs", adminJs);
        Assert.Contains(".admin-panel", styles);
        Assert.Contains("grid-template-columns: repeat(7, minmax(0, 1fr))", styles);
    }

    private static async Task<AuthResponse> RegisterSpecialistAsync(HttpClient client)
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterSpecialistRequest(
            "Jane Doe",
            email,
            "Password1",
            "Password1",
            "+15550101010"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("Auth response was empty.");
    }

    private static async Task<AdminAuthResponse> LoginAdminAsync(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync("/api/v1/admin/auth/login", new AdminLoginRequest(
            "admin@minicrm",
            "Admin12345"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AdminAuthResponse>()
            ?? throw new InvalidOperationException("Admin auth response was empty.");
    }

    private static async Task<BookingSetup> CreatePublicBookingSetupAsync(HttpClient client)
    {
        var auth = await RegisterSpecialistAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var locationResponse = await client.PostAsJsonAsync("/api/v1/locations", new UpsertLocationRequest(
            "Central Office",
            "10 Main Street",
            "Downtown"));
        locationResponse.EnsureSuccessStatusCode();
        var location = await locationResponse.Content.ReadFromJsonAsync<LocationResponse>()
            ?? throw new InvalidOperationException("Location response was empty.");

        var serviceResponse = await client.PostAsJsonAsync("/api/v1/services", new UpsertServiceRequest(
            "Consultation",
            "Initial appointment"));
        serviceResponse.EnsureSuccessStatusCode();
        var service = await serviceResponse.Content.ReadFromJsonAsync<ServiceResponse>()
            ?? throw new InvalidOperationException("Service response was empty.");

        var profileResponse = await client.PutAsJsonAsync("/api/v1/profile", new
        {
            FullName = "Jane Doe",
            Phone = "+15550101010",
            VenueName = "Central Office",
            LocationId = location.Id
        });
        profileResponse.EnsureSuccessStatusCode();

        var specialistServiceResponse = await client.PostAsJsonAsync("/api/v1/specialist-services", new
        {
            ServiceId = service.Id,
            Price = 150m,
            DurationMinutes = 60
        });
        specialistServiceResponse.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = null;
        return new BookingSetup(auth.SpecialistId, service.Id);
    }

    private static async Task<BookingResponse> CreateBookingAsync(HttpClient client, Guid specialistId, Guid serviceId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/bookings", new CreateBookingRequest(
            "Alice Brown",
            "+15550909090",
            specialistId,
            [serviceId],
            new DateOnly(2026, 7, 14),
            new TimeOnly(10, 0),
            "Stage 8 booking"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BookingResponse>()
            ?? throw new InvalidOperationException("Booking response was empty.");
    }

    private sealed record BookingSetup(Guid SpecialistId, Guid ServiceId);
}
