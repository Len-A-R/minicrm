using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Bookings;
using ServiceBooking.Application.Catalog;
using ServiceBooking.Application.Clients;
using ServiceBooking.Application.SpecialistBookings;
using ServiceBooking.Application.SpecialistServices;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Infrastructure.Persistence;
using DomainBooking = ServiceBooking.Domain.Entities.Booking;

namespace ServiceBooking.Tests.Integration;

public sealed class Stage4BookingEndpointTests
{
    [Fact]
    public async Task PublicBookingFlow_CreatesBookingAndClient()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var setup = await CreatePublicBookingSetupAsync(client);

        var specialists = await client.GetFromJsonAsync<IReadOnlyCollection<ServiceBooking.Application.Specialists.PublicSpecialistResponse>>(
            $"/api/v1/specialists?locationId={setup.LocationId}&serviceId={setup.ServiceId}");
        Assert.Single(specialists!);
        Assert.Equal(setup.SpecialistId, specialists!.Single().Id);

        var bookingResponse = await client.PostAsJsonAsync("/api/v1/bookings", new CreateBookingRequest(
            "Alice Brown",
            "+15550909090",
            setup.SpecialistId,
            [setup.ServiceId],
            new DateOnly(2026, 7, 12),
            new TimeOnly(13, 30),
            "Window seat"));

        Assert.Equal(HttpStatusCode.Created, bookingResponse.StatusCode);
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.NotNull(booking);
        Assert.Equal(150m, booking!.TotalPrice);
        Assert.Equal(60, booking.TotalDuration);
        Assert.Single(booking.Services);
        Assert.NotNull(booking.ClientId);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServiceBookingDbContext>();
        Assert.Single(dbContext.Clients.Where(clientEntity => clientEntity.Phone == "+15550909090"));
        Assert.Single(dbContext.Bookings.Where(item => item.ClientPhone == "+15550909090"));
    }

    [Fact]
    public async Task PublicBookingFlow_ReusesExistingClientAndAllowsMessageOnly()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var setup = await CreatePublicBookingSetupAsync(client);

        var firstResponse = await client.PostAsJsonAsync("/api/v1/bookings", new CreateBookingRequest(
            "Alice Brown",
            "+15550909090",
            setup.SpecialistId,
            [setup.ServiceId],
            new DateOnly(2026, 7, 12),
            new TimeOnly(13, 30),
            null));
        firstResponse.EnsureSuccessStatusCode();

        var secondResponse = await client.PostAsJsonAsync("/api/v1/bookings", new CreateBookingRequest(
            "Alice B",
            "+15550909090",
            setup.SpecialistId,
            [],
            new DateOnly(2026, 7, 13),
            new TimeOnly(14, 0),
            "Please call me"));
        secondResponse.EnsureSuccessStatusCode();
        var secondBooking = await secondResponse.Content.ReadFromJsonAsync<BookingResponse>();

        Assert.Empty(secondBooking!.Services);
        Assert.Equal(0m, secondBooking.TotalPrice);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServiceBookingDbContext>();
        var clientEntity = dbContext.Clients.Single(client => client.Phone == "+15550909090");
        Assert.Equal("Alice B", clientEntity.FullName);
        Assert.Equal(2, dbContext.Bookings.Count(item => item.ClientPhone == "+15550909090"));
    }

    [Fact]
    public async Task PublicBookingFlow_ReturnsErrorsForInvalidRequests()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var setup = await CreatePublicBookingSetupAsync(client);

        var emptyBooking = await client.PostAsJsonAsync("/api/v1/bookings", new CreateBookingRequest(
            "Alice Brown",
            "+15550909090",
            setup.SpecialistId,
            [],
            new DateOnly(2026, 7, 12),
            new TimeOnly(13, 30),
            null));
        var invalidService = await client.PostAsJsonAsync("/api/v1/bookings", new CreateBookingRequest(
            "Alice Brown",
            "+15550909090",
            setup.SpecialistId,
            [Guid.NewGuid()],
            new DateOnly(2026, 7, 12),
            new TimeOnly(13, 30),
            null));
        var missingSpecialist = await client.PostAsJsonAsync("/api/v1/bookings", new CreateBookingRequest(
            "Alice Brown",
            "+15550909090",
            Guid.NewGuid(),
            [setup.ServiceId],
            new DateOnly(2026, 7, 12),
            new TimeOnly(13, 30),
            null));

        Assert.Equal(HttpStatusCode.BadRequest, emptyBooking.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidService.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingSpecialist.StatusCode);
    }

    [Fact]
    public async Task ClientPortalFlow_UsesClientProfileAndReturnsHistoryAndNotifications()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var setup = await CreatePublicBookingSetupAsync(client);

        var registerClientResponse = await client.PostAsJsonAsync("/api/v1/auth/register/client", new RegisterClientRequest(
            "Alice Brown",
            $"{Guid.NewGuid():N}@example.com",
            "+15550909090",
            "Password1",
            "Password1"));
        registerClientResponse.EnsureSuccessStatusCode();
        var clientAuth = await registerClientResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal("Client", clientAuth!.Role);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientAuth.AccessToken);
        var me = await client.GetFromJsonAsync<ClientMeResponse>("/api/v1/client/me");
        var updateProfileResponse = await client.PutAsJsonAsync("/api/v1/client/me", new UpdateClientProfileRequest(
            "Alice Updated",
            "+15550909091"));
        updateProfileResponse.EnsureSuccessStatusCode();
        var updatedMe = await updateProfileResponse.Content.ReadFromJsonAsync<ClientMeResponse>();
        var bookingResponse = await client.PostAsJsonAsync("/api/v1/client/bookings", new CreateClientBookingRequest(
            setup.SpecialistId,
            [setup.ServiceId],
            new DateOnly(2026, 7, 12),
            new TimeOnly(13, 30),
            "Need a callback"));
        bookingResponse.EnsureSuccessStatusCode();
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingResponse>();
        var history = await client.GetFromJsonAsync<IReadOnlyCollection<ClientBookingHistoryResponse>>("/api/v1/client/bookings");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setup.SpecialistAccessToken);
        var replyResponse = await client.PostAsJsonAsync(
            $"/api/v1/specialist/bookings/{booking!.Id}/reply",
            new ReplyBookingRequest("Подтверждаю заявку, ожидаю вас."));
        replyResponse.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientAuth.AccessToken);
        var notifications = await client.GetFromJsonAsync<IReadOnlyCollection<ClientNotificationResponse>>("/api/v1/client/notifications");

        Assert.Equal("Alice Brown", me!.FullName);
        Assert.Equal("+15550909090", me.Phone);
        Assert.Equal("Alice Updated", updatedMe!.FullName);
        Assert.Equal("+15550909091", updatedMe.Phone);
        Assert.Equal("Alice Updated", booking!.ClientName);
        Assert.Equal("+15550909091", booking.ClientPhone);
        var historyItem = Assert.Single(history!);
        var notification = Assert.Single(notifications!);
        Assert.Equal("Jane Doe", historyItem.SpecialistName);
        Assert.Equal(150m, historyItem.TotalPrice);
        Assert.Equal("Подтверждаю заявку, ожидаю вас.", notification.Reply);
    }

    [Fact]
    public async Task StaticBookingFrontend_IsServed()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var index = await client.GetStringAsync("/");
        var script = await client.GetStringAsync("/app.js");
        var styles = await client.GetStringAsync("/styles.css");
        var dashboard = await client.GetStringAsync("/dashboard.html");
        var dashboardScript = await client.GetStringAsync("/dashboard.js");
        var login = await client.GetStringAsync("/login");
        var loginScript = await client.GetStringAsync("/login.js");
        var clientPortal = await client.GetStringAsync("/client.html");
        var clientScript = await client.GetStringAsync("/client.js");
        var register = await client.GetStringAsync("/register.html");
        var registerScript = await client.GetStringAsync("/register.js");

        Assert.Contains("Бронирование услуги", index);
        Assert.Contains("/api/v1/bookings", script);
        Assert.Contains("normalizeBookingTime", script);
        Assert.Contains(".step-card.active", styles);
        Assert.Contains("Кабинет специалиста", dashboard);
        Assert.Contains("id=\"dashboard-sidebar\" hidden", dashboard);
        Assert.Contains("id=\"dashboard-nav\"", dashboard);
        Assert.Contains("chart.js", dashboard);
        Assert.DoesNotContain("dashboard-topbar", dashboard);
        Assert.DoesNotContain("id=\"refresh-button\"", dashboard);
        Assert.DoesNotContain("id=\"logout-button\"", dashboard);
        Assert.Contains("specialistMenuItems", dashboardScript);
        Assert.Contains("route: \"profile\"", dashboardScript);
        Assert.Contains("route: \"services\"", dashboardScript);
        Assert.Contains("route: \"reports\"", dashboardScript);
        Assert.Contains("startDashboardAutoRefresh", dashboardScript);
        Assert.Contains("dashboardNavIcon", dashboardScript);
        Assert.Contains("data-dashboard-logout", dashboardScript);
        Assert.Contains("location.href = \"/login\"", dashboardScript);
        Assert.Contains("/api/v1/profile", dashboardScript);
        Assert.Contains("/api/v1/locations", dashboardScript);
        Assert.Contains("/api/v1/specialist/bookings", dashboardScript);
        Assert.Contains("/api/v1/specialist-services", dashboardScript);
        Assert.Contains("/api/v1/calendar", dashboardScript);
        Assert.Contains("/api/v1/kanban", dashboardScript);
        Assert.Contains("/api/v1/reports/summary", dashboardScript);
        Assert.Contains(".profile-editor", styles);
        Assert.Contains(".service-manager", styles);
        Assert.Contains(".kanban-board", styles);
        Assert.Contains(".calendar-grid", styles);
        Assert.Contains(".chart-grid", styles);
        Assert.Contains(".summary-card", styles);
        Assert.Contains(".client-history-card", styles);
        Assert.Contains(".client-notification-card", styles);
        Assert.Contains(".client-profile-form", styles);
        Assert.Contains(".app-nav-item", styles);
        Assert.Contains(".specialist-body .dashboard-nav", styles);
        Assert.Contains(".admin-body .dashboard-nav", styles);
        Assert.Contains(".client-booking-panel #back-button", styles);
        Assert.Contains("bottom: calc(64px + env(safe-area-inset-bottom))", styles);
        Assert.Contains(".client-booking-panel .booking-header", styles);
        Assert.Contains("display: none", styles);
        Assert.Contains("position: fixed", styles);
        Assert.Contains("grid-template-columns: repeat(4, minmax(0, 1fr))", styles);
        Assert.Contains("grid-template-columns: repeat(7, minmax(0, 1fr))", styles);
        Assert.Contains("Вход в систему", login);
        Assert.DoesNotContain("data-login-role", login);
        Assert.Contains("Запомнить меня", login);
        Assert.Contains("href=\"/register.html\"", login);
        Assert.Contains("/api/v1/auth/login", loginScript);
        Assert.Contains("redirectByRole", loginScript);
        Assert.Contains("Кабинет клиента", clientPortal);
        Assert.Contains("id=\"client-sidebar\" hidden", clientPortal);
        Assert.Contains("id=\"client-profile-form\"", clientPortal);
        Assert.Contains("id=\"client-logout-button\"", clientPortal);
        Assert.DoesNotContain("dashboard-topbar", clientPortal);
        Assert.DoesNotContain("client-title", clientPortal);
        Assert.DoesNotContain("client-refresh-button", clientPortal);
        Assert.Contains("data-client-section=\"profile\"", clientPortal);
        Assert.Contains("/api/v1/client/bookings", clientScript);
        Assert.Contains("/api/v1/client/notifications", clientScript);
        Assert.Contains("/api/v1/client/me", clientScript);
        Assert.Contains("route: \"profile\"", clientScript);
        Assert.Contains("startClientAutoRefresh", clientScript);
        Assert.Contains("window.setInterval", clientScript);
        Assert.Contains("Регистрация специалиста", register);
        Assert.Contains("Регистрация клиента", register);
        Assert.Contains("/api/v1/auth/register", registerScript);
        Assert.Contains("/api/v1/auth/register/client", registerScript);
    }

    private static async Task<BookingSetup> CreatePublicBookingSetupAsync(HttpClient client)
    {
        var auth = await RegisterAsync(client);
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

        return new BookingSetup(auth.SpecialistId, auth.AccessToken, location.Id, service.Id);
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterSpecialistRequest(
            "Jane Doe",
            $"{Guid.NewGuid():N}@example.com",
            "Password1",
            "Password1",
            "+15550101010"));
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("Auth response was empty.");
    }

    private sealed record BookingSetup(Guid SpecialistId, string SpecialistAccessToken, Guid LocationId, Guid ServiceId);
}
