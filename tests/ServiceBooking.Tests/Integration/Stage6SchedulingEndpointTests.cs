using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Bookings;
using ServiceBooking.Application.Calendar;
using ServiceBooking.Application.Catalog;
using ServiceBooking.Application.Kanban;
using ServiceBooking.Application.SpecialistBookings;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Tests.Integration;

public sealed class Stage6SchedulingEndpointTests
{
    [Fact]
    public async Task CalendarFlow_ListsReschedulesAndCancelsBooking()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(client);
        var booking = await CreateBookingAsync(client, setup.SpecialistId, setup.ServiceId, new TimeOnly(10, 0));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setup.AccessToken);
        await ConfirmAsync(client, booking.Id, new DateOnly(2026, 7, 14), new TimeOnly(10, 0));

        var calendar = await client.GetFromJsonAsync<IReadOnlyCollection<CalendarBookingResponse>>(
            "/api/v1/calendar?from=2026-07-01&to=2026-07-31");
        Assert.NotNull(calendar);
        Assert.Single(calendar!);
        Assert.Equal(new TimeOnly(10, 0), calendar.Single().StartTime);

        var rescheduleResponse = await client.PutAsJsonAsync(
            $"/api/v1/calendar/{booking.Id}/reschedule",
            new RescheduleBookingRequest(new DateOnly(2026, 7, 15), new TimeOnly(14, 30)));
        rescheduleResponse.EnsureSuccessStatusCode();
        var rescheduled = await rescheduleResponse.Content.ReadFromJsonAsync<CalendarBookingResponse>();
        Assert.Equal(new DateOnly(2026, 7, 15), rescheduled?.Date);
        Assert.Equal(new TimeOnly(14, 30), rescheduled?.StartTime);

        var deleteResponse = await client.DeleteAsync($"/api/v1/calendar/{booking.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterCancel = await client.GetFromJsonAsync<IReadOnlyCollection<CalendarBookingResponse>>(
            "/api/v1/calendar?from=2026-07-01&to=2026-07-31");
        Assert.Empty(afterCancel!);
    }

    [Fact]
    public async Task CalendarReschedule_ReturnsConflictForOverlap()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(client);
        var first = await CreateBookingAsync(client, setup.SpecialistId, setup.ServiceId, new TimeOnly(10, 0));
        var second = await CreateBookingAsync(client, setup.SpecialistId, setup.ServiceId, new TimeOnly(12, 0));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setup.AccessToken);
        await ConfirmAsync(client, first.Id, new DateOnly(2026, 7, 14), new TimeOnly(10, 0));
        await ConfirmAsync(client, second.Id, new DateOnly(2026, 7, 14), new TimeOnly(12, 0));

        var response = await client.PutAsJsonAsync(
            $"/api/v1/calendar/{second.Id}/reschedule",
            new RescheduleBookingRequest(new DateOnly(2026, 7, 14), new TimeOnly(10, 30)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task KanbanFlow_GroupsAndMovesBooking()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(client);
        var booking = await CreateBookingAsync(client, setup.SpecialistId, setup.ServiceId, new TimeOnly(10, 0));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setup.AccessToken);

        var board = await client.GetFromJsonAsync<KanbanBoardResponse>("/api/v1/kanban?date=2026-07-14");
        Assert.NotNull(board);
        Assert.Equal(4, board!.Columns.Count);
        Assert.Single(board.Columns.Single(column => column.Status == BookingStatus.New).Items);

        var confirmMove = await client.PutAsJsonAsync(
            $"/api/v1/kanban/{booking.Id}/move",
            new MoveKanbanBookingRequest(BookingStatus.Confirmed));
        confirmMove.EnsureSuccessStatusCode();
        var confirmedCard = await confirmMove.Content.ReadFromJsonAsync<KanbanBookingCardResponse>();
        Assert.Equal(booking.Id, confirmedCard?.Id);

        var completedMove = await client.PutAsJsonAsync(
            $"/api/v1/kanban/{booking.Id}/move",
            new MoveKanbanBookingRequest(BookingStatus.Completed));
        completedMove.EnsureSuccessStatusCode();

        var updatedBoard = await client.GetFromJsonAsync<KanbanBoardResponse>("/api/v1/kanban?date=2026-07-14");
        Assert.Single(updatedBoard!.Columns.Single(column => column.Status == BookingStatus.Completed).Items);
    }

    [Fact]
    public async Task SchedulingEndpoints_RequireAuthentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var calendar = await client.GetAsync("/api/v1/calendar?from=2026-07-01&to=2026-07-31");
        var kanban = await client.GetAsync("/api/v1/kanban?date=2026-07-14");

        Assert.Equal(HttpStatusCode.Unauthorized, calendar.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, kanban.StatusCode);
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
            new DateOnly(2026, 7, 14),
            time,
            "First visit"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BookingResponse>()
            ?? throw new InvalidOperationException("Booking response was empty.");
    }

    private static async Task ConfirmAsync(HttpClient client, Guid bookingId, DateOnly date, TimeOnly time)
    {
        var response = await client.PutAsJsonAsync(
            $"/api/v1/specialist/bookings/{bookingId}/confirm",
            new ConfirmBookingRequest(date, time));
        response.EnsureSuccessStatusCode();
    }

    private sealed record Setup(Guid SpecialistId, string AccessToken, Guid ServiceId);
}
