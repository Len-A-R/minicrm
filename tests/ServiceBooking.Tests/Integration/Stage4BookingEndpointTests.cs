using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Bookings;
using ServiceBooking.Application.Catalog;
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
    public async Task StaticBookingFrontend_IsServed()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var index = await client.GetStringAsync("/");
        var script = await client.GetStringAsync("/app.js");
        var styles = await client.GetStringAsync("/styles.css");
        var dashboard = await client.GetStringAsync("/dashboard.html");
        var dashboardScript = await client.GetStringAsync("/dashboard.js");

        Assert.Contains("Бронирование услуги", index);
        Assert.Contains("/api/v1/bookings", script);
        Assert.Contains(".step-card.active", styles);
        Assert.Contains("Кабинет специалиста", dashboard);
        Assert.Contains("/api/v1/specialist/bookings", dashboardScript);
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

        return new BookingSetup(auth.SpecialistId, location.Id, service.Id);
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

    private sealed record BookingSetup(Guid SpecialistId, Guid LocationId, Guid ServiceId);
}
