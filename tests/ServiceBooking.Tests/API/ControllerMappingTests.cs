using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.API.Controllers;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Common;
using ServiceBooking.Application.Profile;

namespace ServiceBooking.Tests.API;

public sealed class ControllerMappingTests
{
    [Fact]
    public async Task AuthGetMe_ReturnsUnauthorizedWhenTokenHasNoSpecialistId()
    {
        var controller = new AuthController(new FakeAuthService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.GetMe(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task AuthGetMe_MapsNotFoundFromService()
    {
        var controller = new AuthController(new FakeAuthService { ReturnNotFound = true });
        controller.ControllerContext = CreateControllerContext(Guid.NewGuid());

        var result = await controller.GetMe(CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task ProfileGet_ReturnsUnauthorizedWhenTokenHasNoSpecialistId()
    {
        var controller = new SpecialistProfileController(new FakeProfileService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.GetProfile(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task ProfileGet_MapsNotFoundFromService()
    {
        var controller = new SpecialistProfileController(new FakeProfileService { ReturnNotFound = true });
        controller.ControllerContext = CreateControllerContext(Guid.NewGuid());

        var result = await controller.GetProfile(CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UploadAvatar_ReturnsBadRequestWhenFileMissing()
    {
        var controller = new SpecialistProfileController(new FakeProfileService());
        controller.ControllerContext = CreateControllerContext(Guid.NewGuid());

        var result = await controller.UploadAvatar(null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    private static ControllerContext CreateControllerContext(Guid specialistId)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, specialistId.ToString())],
            "Test"));

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    private sealed class FakeAuthService : IAuthService
    {
        public bool ReturnNotFound { get; init; }

        public Task<ServiceResult<AuthResponse>> RegisterAsync(
            RegisterSpecialistRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ServiceResult<AuthResponse>.Failure(
                ResultStatus.Validation,
                "not_used",
                "Not used."));
        }

        public Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(ServiceResult<AuthResponse>.Failure(
                ResultStatus.Unauthorized,
                "not_used",
                "Not used."));
        }

        public Task<ServiceResult<AuthResponse>> RefreshAsync(
            RefreshTokenRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ServiceResult<AuthResponse>.Failure(
                ResultStatus.Unauthorized,
                "not_used",
                "Not used."));
        }

        public Task<ServiceResult<SpecialistMeResponse>> GetMeAsync(
            Guid specialistId,
            CancellationToken cancellationToken)
        {
            if (ReturnNotFound)
            {
                return Task.FromResult(ServiceResult<SpecialistMeResponse>.Failure(
                    ResultStatus.NotFound,
                    "specialist_not_found",
                    "Specialist was not found."));
            }

            return Task.FromResult(ServiceResult<SpecialistMeResponse>.Success(new SpecialistMeResponse(
                specialistId,
                "Jane Doe",
                "jane@example.com",
                "+15550101010",
                null,
                null,
                null)));
        }
    }

    private sealed class FakeProfileService : IProfileService
    {
        public bool ReturnNotFound { get; init; }

        public Task<ServiceResult<ProfileResponse>> GetProfileAsync(
            Guid specialistId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(CreateProfileResult(specialistId));
        }

        public Task<ServiceResult<ProfileResponse>> UpdateProfileAsync(
            Guid specialistId,
            UpdateSpecialistProfileRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(CreateProfileResult(specialistId));
        }

        public Task<ServiceResult<ProfileResponse>> UploadAvatarAsync(
            Guid specialistId,
            AvatarUploadRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(CreateProfileResult(specialistId));
        }

        private ServiceResult<ProfileResponse> CreateProfileResult(Guid specialistId)
        {
            if (ReturnNotFound)
            {
                return ServiceResult<ProfileResponse>.Failure(
                    ResultStatus.NotFound,
                    "specialist_not_found",
                    "Specialist was not found.");
            }

            return ServiceResult<ProfileResponse>.Success(new ProfileResponse(
                specialistId,
                "Jane Doe",
                "jane@example.com",
                "+15550101010",
                null,
                null,
                null,
                DateTimeOffset.UtcNow));
        }
    }
}
