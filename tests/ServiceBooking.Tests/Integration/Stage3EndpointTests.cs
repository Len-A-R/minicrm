using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Catalog;
using ServiceBooking.Application.Slots;
using ServiceBooking.Application.SpecialistServices;
using ServiceBooking.Application.Vacations;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Tests.Integration;

public sealed class Stage3EndpointTests
{
    [Fact]
    public async Task ServicesAndLocationsCrud_WorksEndToEnd()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client, "catalog-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var locationResponse = await client.PostAsJsonAsync("/api/v1/locations", new UpsertLocationRequest(
            "Central Office",
            "10 Main Street",
            "Downtown"));
        Assert.Equal(HttpStatusCode.Created, locationResponse.StatusCode);
        var location = await locationResponse.Content.ReadFromJsonAsync<LocationResponse>();
        Assert.NotNull(location);

        var serviceResponse = await client.PostAsJsonAsync("/api/v1/services", new UpsertServiceRequest(
            "Consultation",
            "Initial visit"));
        Assert.Equal(HttpStatusCode.Created, serviceResponse.StatusCode);
        var service = await serviceResponse.Content.ReadFromJsonAsync<ServiceResponse>();
        Assert.NotNull(service);

        var publicServices = await client.GetFromJsonAsync<IReadOnlyCollection<ServiceResponse>>("/api/v1/services");
        Assert.Contains(publicServices!, item => item.Id == service!.Id);

        var getService = await client.GetFromJsonAsync<ServiceResponse>($"/api/v1/services/{service.Id}");
        Assert.Equal("Consultation", getService?.Name);

        var missingServiceResponse = await client.GetAsync($"/api/v1/services/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missingServiceResponse.StatusCode);

        var invalidServiceResponse = await client.PostAsJsonAsync("/api/v1/services", new UpsertServiceRequest("", null));
        Assert.Equal(HttpStatusCode.BadRequest, invalidServiceResponse.StatusCode);

        var updatedServiceResponse = await client.PutAsJsonAsync(
            $"/api/v1/services/{service!.Id}",
            new UpsertServiceRequest("Consultation Plus", "Extended visit"));
        updatedServiceResponse.EnsureSuccessStatusCode();
        var updatedService = await updatedServiceResponse.Content.ReadFromJsonAsync<ServiceResponse>();
        Assert.Equal("Consultation Plus", updatedService?.Name);

        var missingServiceUpdateResponse = await client.PutAsJsonAsync(
            $"/api/v1/services/{Guid.NewGuid()}",
            new UpsertServiceRequest("Missing", null));
        Assert.Equal(HttpStatusCode.NotFound, missingServiceUpdateResponse.StatusCode);

        var updatedLocationResponse = await client.PutAsJsonAsync(
            $"/api/v1/locations/{location!.Id}",
            new UpsertLocationRequest("North Office", "20 North Street", null));
        updatedLocationResponse.EnsureSuccessStatusCode();
        var updatedLocation = await updatedLocationResponse.Content.ReadFromJsonAsync<LocationResponse>();
        Assert.Equal("North Office", updatedLocation?.Name);

        var getLocation = await client.GetFromJsonAsync<LocationResponse>($"/api/v1/locations/{location.Id}");
        Assert.Equal("20 North Street", getLocation?.Address);

        var missingLocationResponse = await client.GetAsync($"/api/v1/locations/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missingLocationResponse.StatusCode);

        var invalidLocationResponse = await client.PostAsJsonAsync("/api/v1/locations", new UpsertLocationRequest("", "", null));
        Assert.Equal(HttpStatusCode.BadRequest, invalidLocationResponse.StatusCode);

        var missingLocationUpdateResponse = await client.PutAsJsonAsync(
            $"/api/v1/locations/{Guid.NewGuid()}",
            new UpsertLocationRequest("Missing", "Address", null));
        Assert.Equal(HttpStatusCode.NotFound, missingLocationUpdateResponse.StatusCode);

        var missingLocationDeleteResponse = await client.DeleteAsync($"/api/v1/locations/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missingLocationDeleteResponse.StatusCode);

        var deleteLocationResponse = await client.DeleteAsync($"/api/v1/locations/{location.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteLocationResponse.StatusCode);

        var deleteServiceResponse = await client.DeleteAsync($"/api/v1/services/{service.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteServiceResponse.StatusCode);
    }

    [Fact]
    public async Task SpecialistServicesVacationsAndSlots_WorkEndToEnd()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client, "stage3-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var location = await CreateLocationAsync(client);
        var service = await CreateCatalogServiceAsync(client, "Therapy", "Session");

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
            Price = 100m,
            DurationMinutes = 60
        });
        Assert.Equal(HttpStatusCode.Created, specialistServiceResponse.StatusCode);
        var specialistService = await specialistServiceResponse.Content.ReadFromJsonAsync<SpecialistServiceResponse>();
        Assert.NotNull(specialistService);
        Assert.Equal("Therapy", specialistService!.ServiceName);

        var duplicateSpecialistServiceResponse = await client.PostAsJsonAsync("/api/v1/specialist-services", new
        {
            ServiceId = service.Id,
            Price = 110m,
            DurationMinutes = 60
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicateSpecialistServiceResponse.StatusCode);

        var currentServices = await client.GetFromJsonAsync<IReadOnlyCollection<SpecialistServiceResponse>>(
            "/api/v1/specialist-services");
        Assert.Single(currentServices!);

        var publicSpecialistServices = await client.GetFromJsonAsync<IReadOnlyCollection<SpecialistServiceResponse>>(
            $"/api/v1/specialists/{auth.SpecialistId}/services");
        Assert.Single(publicSpecialistServices!);

        var filteredLocations = await client.GetFromJsonAsync<IReadOnlyCollection<LocationResponse>>(
            $"/api/v1/locations?serviceId={service.Id}");
        Assert.Contains(filteredLocations!, item => item.Id == location.Id);

        var updateSpecialistServiceResponse = await client.PutAsJsonAsync(
            $"/api/v1/specialist-services/{specialistService.Id}",
            new
            {
                ServiceId = service.Id,
                Price = 120m,
                DurationMinutes = 90
            });
        updateSpecialistServiceResponse.EnsureSuccessStatusCode();
        var updatedSpecialistService = await updateSpecialistServiceResponse.Content
            .ReadFromJsonAsync<SpecialistServiceResponse>();
        Assert.Equal(120m, updatedSpecialistService?.Price);
        Assert.Equal(90, updatedSpecialistService?.DurationMinutes);

        var serviceInUseDeleteResponse = await client.DeleteAsync($"/api/v1/services/{service.Id}");
        Assert.Equal(HttpStatusCode.Conflict, serviceInUseDeleteResponse.StatusCode);

        var missingSpecialistServiceUpdate = await client.PutAsJsonAsync(
            $"/api/v1/specialist-services/{Guid.NewGuid()}",
            new
            {
                ServiceId = service.Id,
                Price = 120m,
                DurationMinutes = 60
            });
        Assert.Equal(HttpStatusCode.NotFound, missingSpecialistServiceUpdate.StatusCode);

        var invalidSpecialistServiceResponse = await client.PostAsJsonAsync("/api/v1/specialist-services", new
        {
            ServiceId = Guid.NewGuid(),
            Price = 100m,
            DurationMinutes = 60
        });
        Assert.Equal(HttpStatusCode.NotFound, invalidSpecialistServiceResponse.StatusCode);

        var vacationDate = new DateOnly(2026, 7, 2);
        var vacationResponse = await client.PostAsJsonAsync("/api/v1/vacations", new UpsertVacationRequest(
            vacationDate,
            "Day off"));
        Assert.Equal(HttpStatusCode.Created, vacationResponse.StatusCode);
        var vacation = await vacationResponse.Content.ReadFromJsonAsync<VacationResponse>();
        Assert.NotNull(vacation);

        var duplicateVacationResponse = await client.PostAsJsonAsync("/api/v1/vacations", new UpsertVacationRequest(
            vacationDate,
            "Duplicate"));
        Assert.Equal(HttpStatusCode.Conflict, duplicateVacationResponse.StatusCode);

        var vacations = await client.GetFromJsonAsync<IReadOnlyCollection<VacationResponse>>("/api/v1/vacations");
        Assert.Single(vacations!);

        var updatedVacationDate = new DateOnly(2026, 7, 3);
        var updateVacationResponse = await client.PutAsJsonAsync(
            $"/api/v1/vacations/{vacation!.Id}",
            new UpsertVacationRequest(updatedVacationDate, "Moved day off"));
        updateVacationResponse.EnsureSuccessStatusCode();
        var updatedVacation = await updateVacationResponse.Content.ReadFromJsonAsync<VacationResponse>();
        Assert.Equal(updatedVacationDate, updatedVacation?.Date);

        var missingVacationUpdateResponse = await client.PutAsJsonAsync(
            $"/api/v1/vacations/{Guid.NewGuid()}",
            new UpsertVacationRequest(new DateOnly(2026, 7, 5), null));
        Assert.Equal(HttpStatusCode.NotFound, missingVacationUpdateResponse.StatusCode);

        var missingVacationDeleteResponse = await client.DeleteAsync($"/api/v1/vacations/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missingVacationDeleteResponse.StatusCode);

        var noSlots = await client.GetFromJsonAsync<IReadOnlyCollection<AvailableSlotResponse>>(
            $"/api/v1/specialists/{auth.SpecialistId}/slots?date={updatedVacationDate:yyyy-MM-dd}&durationMinutes=60");
        Assert.Empty(noSlots!);

        var invalidSlotDurationResponse = await client.GetAsync(
            $"/api/v1/specialists/{auth.SpecialistId}/slots?date=2026-07-04&durationMinutes=0");
        Assert.Equal(HttpStatusCode.BadRequest, invalidSlotDurationResponse.StatusCode);

        var missingSpecialistSlotsResponse = await client.GetAsync(
            $"/api/v1/specialists/{Guid.NewGuid()}/slots?date=2026-07-04&durationMinutes=60");
        Assert.Equal(HttpStatusCode.NotFound, missingSpecialistSlotsResponse.StatusCode);

        await AddConfirmedBookingAsync(factory, auth.SpecialistId, service.Id);

        var slots = await client.GetFromJsonAsync<IReadOnlyCollection<AvailableSlotResponse>>(
            $"/api/v1/specialists/{auth.SpecialistId}/slots?date=2026-07-04&durationMinutes=60");
        Assert.NotNull(slots);
        Assert.DoesNotContain(slots!, slot => slot.Time == new TimeOnly(9, 30));
        Assert.DoesNotContain(slots!, slot => slot.Time == new TimeOnly(10, 0));
        Assert.Contains(slots!, slot => slot.Time == new TimeOnly(10, 30));

        var deleteVacationResponse = await client.DeleteAsync($"/api/v1/vacations/{vacation.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteVacationResponse.StatusCode);

        var missingSpecialistServiceDelete = await client.DeleteAsync($"/api/v1/specialist-services/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missingSpecialistServiceDelete.StatusCode);

        var deleteSpecialistServiceResponse = await client.DeleteAsync($"/api/v1/specialist-services/{specialistService.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteSpecialistServiceResponse.StatusCode);
    }

    [Fact]
    public async Task Stage3ProtectedEndpoints_RequireAuthentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var createService = await client.PostAsJsonAsync("/api/v1/services", new UpsertServiceRequest("Name", null));
        var createLocation = await client.PostAsJsonAsync("/api/v1/locations", new UpsertLocationRequest("Name", "Address", null));
        var createSpecialistService = await client.PostAsJsonAsync("/api/v1/specialist-services", new
        {
            ServiceId = Guid.NewGuid(),
            Price = 10m,
            DurationMinutes = 30
        });
        var createVacation = await client.PostAsJsonAsync("/api/v1/vacations", new UpsertVacationRequest(
            new DateOnly(2026, 7, 1),
            null));

        Assert.Equal(HttpStatusCode.Unauthorized, createService.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, createLocation.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, createSpecialistService.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, createVacation.StatusCode);
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, string email)
    {
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

    private static async Task<LocationResponse> CreateLocationAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/locations", new UpsertLocationRequest(
            "Central Office",
            "10 Main Street",
            "Downtown"));
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<LocationResponse>()
            ?? throw new InvalidOperationException("Location response was empty.");
    }

    private static async Task<ServiceResponse> CreateCatalogServiceAsync(
        HttpClient client,
        string name,
        string description)
    {
        var response = await client.PostAsJsonAsync("/api/v1/services", new UpsertServiceRequest(name, description));
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ServiceResponse>()
            ?? throw new InvalidOperationException("Service response was empty.");
    }

    private static async Task AddConfirmedBookingAsync(
        TestWebApplicationFactory factory,
        Guid specialistId,
        Guid serviceId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServiceBookingDbContext>();
        var booking = new Booking(
            "Alice Brown",
            "+15550909090",
            specialistId,
            new DateOnly(2026, 7, 4),
            new TimeOnly(9, 30),
            [new BookingService(serviceId, "Therapy", 100m, 60)]);
        booking.Confirm(new DateOnly(2026, 7, 4), new TimeOnly(9, 30));
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();
    }
}
