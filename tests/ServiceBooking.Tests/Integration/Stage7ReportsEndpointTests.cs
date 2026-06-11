using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Bookings;
using ServiceBooking.Application.Catalog;
using ServiceBooking.Application.Reports;
using ServiceBooking.Application.SpecialistBookings;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Tests.Integration;

public sealed class Stage7ReportsEndpointTests
{
    [Fact]
    public async Task ReportsFlow_ReturnsSummaryAndBreakdownsForCompletedBookings()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(client);
        var booking = await CreateBookingAsync(client, setup.SpecialistId, setup.ServiceAId, setup.ServiceBId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setup.AccessToken);
        var completeResponse = await client.PutAsJsonAsync(
            $"/api/v1/specialist/bookings/{booking.Id}/complete",
            new CompleteBookingRequest(175m));
        completeResponse.EnsureSuccessStatusCode();
        SetCompletedAt(factory, booking.Id, 175m, new DateOnly(2026, 7, 14));

        var summary = await client.GetFromJsonAsync<ReportSummaryResponse>(
            "/api/v1/reports/summary?from=2026-07-01&to=2026-07-31");
        var byService = await client.GetFromJsonAsync<IReadOnlyCollection<RevenueByServiceResponse>>(
            "/api/v1/reports/by-service?from=2026-07-01&to=2026-07-31");
        var byClient = await client.GetFromJsonAsync<IReadOnlyCollection<RevenueByClientResponse>>(
            "/api/v1/reports/by-client?from=2026-07-01&to=2026-07-31");
        var byDay = await client.GetFromJsonAsync<IReadOnlyCollection<RevenueByDayResponse>>(
            "/api/v1/reports/by-day?from=2026-07-01&to=2026-07-31");

        Assert.NotNull(summary);
        Assert.Equal(175m, summary!.TotalRevenue);
        Assert.Equal(1, summary.CompletedBookings);
        Assert.Equal(175m, summary.AverageCheck);
        Assert.Equal(2, byService!.Count);
        Assert.Equal(175m, byService.Sum(item => item.Revenue));
        var clientReport = Assert.Single(byClient!);
        Assert.Equal("Alice Brown", clientReport.ClientName);
        Assert.Equal(31, byDay!.Count);
        Assert.Equal(175m, byDay.Single(item => item.Date == new DateOnly(2026, 7, 14)).Revenue);
        Assert.Equal(0m, byDay.Single(item => item.Date == new DateOnly(2026, 7, 1)).Revenue);
    }

    [Fact]
    public async Task ReportsEndpoints_RequireAuthenticationAndValidatePeriod()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(client);

        var unauthorized = await client.GetAsync("/api/v1/reports/summary?from=2026-07-01&to=2026-07-31");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setup.AccessToken);
        var invalidPeriod = await client.GetAsync("/api/v1/reports/summary?from=2026-08-01&to=2026-07-31");

        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidPeriod.StatusCode);
    }

    private static async Task<Setup> CreateSetupAsync(HttpClient client)
    {
        var authResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterSpecialistRequest(
            "Jane Doe",
            $"{Guid.NewGuid():N}@example.com",
            "Password1",
            "Password1",
            "+15550101010"));
        authResponse.EnsureSuccessStatusCode();
        var auth = await authResponse.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("Auth response was empty.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var locationResponse = await client.PostAsJsonAsync("/api/v1/locations", new UpsertLocationRequest(
            "Central Office",
            "10 Main Street",
            "Downtown"));
        locationResponse.EnsureSuccessStatusCode();
        var location = await locationResponse.Content.ReadFromJsonAsync<LocationResponse>()
            ?? throw new InvalidOperationException("Location response was empty.");

        var firstService = await CreateServiceAsync(client, "Consultation", "Initial appointment");
        var secondService = await CreateServiceAsync(client, "Diagnostics", "Extended diagnostics");

        var profileResponse = await client.PutAsJsonAsync("/api/v1/profile", new
        {
            FullName = "Jane Doe",
            Phone = "+15550101010",
            VenueName = "Central Office",
            LocationId = location.Id
        });
        profileResponse.EnsureSuccessStatusCode();

        await CreateSpecialistServiceAsync(client, firstService.Id, 100m, 30);
        await CreateSpecialistServiceAsync(client, secondService.Id, 150m, 45);

        client.DefaultRequestHeaders.Authorization = null;
        return new Setup(auth.SpecialistId, auth.AccessToken, firstService.Id, secondService.Id);
    }

    private static async Task<ServiceResponse> CreateServiceAsync(HttpClient client, string name, string description)
    {
        var serviceResponse = await client.PostAsJsonAsync("/api/v1/services", new UpsertServiceRequest(name, description));
        serviceResponse.EnsureSuccessStatusCode();
        return await serviceResponse.Content.ReadFromJsonAsync<ServiceResponse>()
            ?? throw new InvalidOperationException("Service response was empty.");
    }

    private static async Task CreateSpecialistServiceAsync(HttpClient client, Guid serviceId, decimal price, int durationMinutes)
    {
        var response = await client.PostAsJsonAsync("/api/v1/specialist-services", new
        {
            ServiceId = serviceId,
            Price = price,
            DurationMinutes = durationMinutes
        });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<BookingResponse> CreateBookingAsync(
        HttpClient client,
        Guid specialistId,
        Guid serviceAId,
        Guid serviceBId)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync("/api/v1/bookings", new CreateBookingRequest(
            "Alice Brown",
            "+15550909090",
            specialistId,
            [serviceAId, serviceBId],
            new DateOnly(2026, 7, 14),
            new TimeOnly(10, 0),
            "Stage 7 report booking"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BookingResponse>()
            ?? throw new InvalidOperationException("Booking response was empty.");
    }

    private static void SetCompletedAt(TestWebApplicationFactory factory, Guid bookingId, decimal revenue, DateOnly completedDate)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServiceBookingDbContext>();
        var booking = dbContext.Bookings.Single(item => item.Id == bookingId);
        booking.Complete(revenue, new DateTimeOffset(completedDate, new TimeOnly(12, 0), TimeSpan.Zero));
        dbContext.SaveChanges();
    }

    private sealed record Setup(Guid SpecialistId, string AccessToken, Guid ServiceAId, Guid ServiceBId);
}
