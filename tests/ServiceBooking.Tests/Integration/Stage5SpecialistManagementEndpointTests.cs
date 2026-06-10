using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Bookings;
using ServiceBooking.Application.Catalog;
using ServiceBooking.Application.SpecialistBookings;
using ServiceBooking.Application.SpecialistClients;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Tests.Integration;

public sealed class Stage5SpecialistManagementEndpointTests
{
    [Fact]
    public async Task SpecialistBookingsFlow_ListsAndUpdatesBookingActions()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(client);
        var booking = await CreateBookingAsync(client, setup.SpecialistId, setup.ServiceId, new TimeOnly(10, 0));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setup.AccessToken);

        var list = await client.GetFromJsonAsync<PagedBookingResponse>("/api/v1/specialist/bookings?status=New&page=1&pageSize=10");
        Assert.NotNull(list);
        Assert.Single(list!.Items);
        Assert.Equal(booking.Id, list.Items.Single().Id);

        var replyResponse = await client.PostAsJsonAsync(
            $"/api/v1/specialist/bookings/{booking.Id}/reply",
            new ReplyBookingRequest("Confirmed after call"));
        replyResponse.EnsureSuccessStatusCode();
        var replied = await replyResponse.Content.ReadFromJsonAsync<SpecialistBookingResponse>();
        Assert.Equal("Confirmed after call", replied?.SpecialistReply);

        var confirmResponse = await client.PutAsJsonAsync(
            $"/api/v1/specialist/bookings/{booking.Id}/confirm",
            new ConfirmBookingRequest(new DateOnly(2026, 7, 12), new TimeOnly(10, 0)));
        confirmResponse.EnsureSuccessStatusCode();
        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<SpecialistBookingResponse>();
        Assert.Equal(BookingStatus.Confirmed, confirmed?.Status);
        Assert.Equal(new TimeOnly(10, 0), confirmed?.ConfirmedTime);

        var completeResponse = await client.PutAsJsonAsync(
            $"/api/v1/specialist/bookings/{booking.Id}/complete",
            new CompleteBookingRequest(180m));
        completeResponse.EnsureSuccessStatusCode();
        var completed = await completeResponse.Content.ReadFromJsonAsync<SpecialistBookingResponse>();
        Assert.Equal(BookingStatus.Completed, completed?.Status);
        Assert.Equal(180m, completed?.ActualRevenue);
    }

    [Fact]
    public async Task Confirm_ReturnsConflictForOverlappingBooking()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(client);
        var first = await CreateBookingAsync(client, setup.SpecialistId, setup.ServiceId, new TimeOnly(10, 0));
        var second = await CreateBookingAsync(client, setup.SpecialistId, setup.ServiceId, new TimeOnly(10, 30));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setup.AccessToken);

        var firstConfirm = await client.PutAsJsonAsync(
            $"/api/v1/specialist/bookings/{first.Id}/confirm",
            new ConfirmBookingRequest(new DateOnly(2026, 7, 12), new TimeOnly(10, 0)));
        firstConfirm.EnsureSuccessStatusCode();

        var secondConfirm = await client.PutAsJsonAsync(
            $"/api/v1/specialist/bookings/{second.Id}/confirm",
            new ConfirmBookingRequest(new DateOnly(2026, 7, 12), new TimeOnly(10, 30)));

        Assert.Equal(HttpStatusCode.Conflict, secondConfirm.StatusCode);
    }

    [Fact]
    public async Task SpecialistClientsFlow_ListsAndUpdatesClientStatusAndTag()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(client);
        await CreateBookingAsync(client, setup.SpecialistId, setup.ServiceId, new TimeOnly(10, 0));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setup.AccessToken);

        var clients = await client.GetFromJsonAsync<IReadOnlyCollection<SpecialistClientResponse>>("/api/v1/specialist/clients");
        Assert.NotNull(clients);
        var specialistClient = clients!.Single();
        Assert.Equal("Alice Brown", specialistClient.FullName);
        Assert.Equal(1, specialistClient.BookingCount);

        var statusResponse = await client.PutAsJsonAsync(
            $"/api/v1/specialist/clients/{specialistClient.Id}/status",
            new UpdateClientStatusRequest(ClientStatus.Vip));
        statusResponse.EnsureSuccessStatusCode();
        var updatedStatus = await statusResponse.Content.ReadFromJsonAsync<SpecialistClientResponse>();
        Assert.Equal(ClientStatus.Vip, updatedStatus?.Status);

        var tagResponse = await client.PutAsJsonAsync(
            $"/api/v1/specialist/clients/{specialistClient.Id}/tag",
            new UpdateClientTagRequest("Prefers morning"));
        tagResponse.EnsureSuccessStatusCode();
        var updatedTag = await tagResponse.Content.ReadFromJsonAsync<SpecialistClientResponse>();
        Assert.Equal("Prefers morning", updatedTag?.Tag);
    }

    [Fact]
    public async Task SpecialistManagementEndpoints_RequireAuthentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var bookings = await client.GetAsync("/api/v1/specialist/bookings");
        var clients = await client.GetAsync("/api/v1/specialist/clients");

        Assert.Equal(HttpStatusCode.Unauthorized, bookings.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, clients.StatusCode);
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
        return new Setup(auth.SpecialistId, auth.AccessToken, service.Id);
    }

    private static async Task<BookingResponse> CreateBookingAsync(
        HttpClient client,
        Guid specialistId,
        Guid serviceId,
        TimeOnly time)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync("/api/v1/bookings", new CreateBookingRequest(
            "Alice Brown",
            "+15550909090",
            specialistId,
            [serviceId],
            new DateOnly(2026, 7, 12),
            time,
            "First visit"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BookingResponse>()
            ?? throw new InvalidOperationException("Booking response was empty.");
    }

    private sealed record Setup(Guid SpecialistId, string AccessToken, Guid ServiceId);
}
