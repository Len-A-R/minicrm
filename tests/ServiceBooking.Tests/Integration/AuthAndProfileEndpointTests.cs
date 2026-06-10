using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Profile;

namespace ServiceBooking.Tests.Integration;

public sealed class AuthAndProfileEndpointTests
{
    [Fact]
    public async Task AuthAndProfileFlow_WorksEndToEnd()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterSpecialistRequest(
            "Jane Doe",
            "jane@example.com",
            "Password1",
            "Password1",
            "+15550101010"));

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.NotEqual(Guid.Empty, auth!.SpecialistId);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var me = await client.GetFromJsonAsync<SpecialistMeResponse>("/api/v1/auth/me");
        Assert.Equal("Jane Doe", me?.FullName);

        var updateResponse = await client.PutAsJsonAsync("/api/v1/profile", new UpdateSpecialistProfileRequest(
            "Jane Smith",
            "+15550202020",
            "Main Studio",
            null));

        updateResponse.EnsureSuccessStatusCode();
        var updatedProfile = await updateResponse.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal("Jane Smith", updatedProfile?.FullName);
        Assert.Equal("Main Studio", updatedProfile?.VenueName);

        var profile = await client.GetFromJsonAsync<ProfileResponse>("/api/v1/profile");
        Assert.Equal("Jane Smith", profile?.FullName);

        var invalidProfileResponse = await client.PutAsJsonAsync("/api/v1/profile", new UpdateSpecialistProfileRequest(
            "J",
            "+15550202020",
            null,
            null));
        Assert.Equal(HttpStatusCode.BadRequest, invalidProfileResponse.StatusCode);

        using var multipart = new MultipartFormDataContent();
        using var avatarContent = new ByteArrayContent([137, 80, 78, 71]);
        avatarContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        multipart.Add(avatarContent, "avatar", "avatar.png");

        var avatarResponse = await client.PostAsync("/api/v1/profile/avatar", multipart);
        avatarResponse.EnsureSuccessStatusCode();
        var avatarProfile = await avatarResponse.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal("/test-avatars/avatar.png", avatarProfile?.AvatarUrl);

        using var invalidMultipart = new MultipartFormDataContent();
        using var invalidAvatarContent = new ByteArrayContent([1, 2, 3, 4]);
        invalidAvatarContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        invalidMultipart.Add(invalidAvatarContent, "avatar", "avatar.txt");
        var invalidAvatarResponse = await client.PostAsync("/api/v1/profile/avatar", invalidMultipart);
        Assert.Equal(HttpStatusCode.BadRequest, invalidAvatarResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(
            auth.SpecialistId,
            auth.RefreshToken));

        refreshResponse.EnsureSuccessStatusCode();
        var refreshedAuth = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(refreshedAuth);
        Assert.False(string.IsNullOrWhiteSpace(refreshedAuth!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshedAuth.RefreshToken));
    }

    [Fact]
    public async Task Register_ReturnsConflictForDuplicateEmail()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = new RegisterSpecialistRequest(
            "Jane Doe",
            "jane@example.com",
            "Password1",
            "Password1",
            "+15550101010");

        var firstResponse = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        var secondResponse = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorizedForInvalidCredentials()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(
            "missing@example.com",
            "Password1"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_RequiresAuthentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
