using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Common;
using ServiceBooking.Application.Specialists;
using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Tests.Application;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesSpecialistAndIssuesTokens()
    {
        var repository = new FakeSpecialistRepository();
        var service = CreateService(repository);

        var result = await service.RegisterAsync(new RegisterSpecialistRequest(
            "Jane Doe",
            "Jane@Example.com",
            "Password1",
            "Password1",
            "+15550101010"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("jane@example.com", result.Value?.Email);
        Assert.Equal("access-token", result.Value?.AccessToken);
        Assert.Equal("refresh-token", result.Value?.RefreshToken);
        Assert.Single(repository.Specialists);
        Assert.Equal("hash:Password1", repository.Specialists[0].PasswordHash);
        Assert.Equal("hash:refresh-token", repository.Specialists[0].RefreshTokenHash);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsConflictForDuplicateEmail()
    {
        var repository = new FakeSpecialistRepository();
        repository.Specialists.Add(new Specialist("Jane Doe", "jane@example.com", "+15550101010", "hash:Password1"));
        var service = CreateService(repository);

        var result = await service.RegisterAsync(new RegisterSpecialistRequest(
            "Jane Doe",
            "jane@example.com",
            "Password1",
            "Password1",
            "+15550101010"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Theory]
    [InlineData("bad-email", "Password1", "Password1", "invalid_email")]
    [InlineData("jane@example.com", "password1", "password1", "weak_password")]
    [InlineData("jane@example.com", "Password1", "Password2", "password_mismatch")]
    public async Task RegisterAsync_ValidatesInput(
        string email,
        string password,
        string confirmPassword,
        string expectedCode)
    {
        var service = CreateService(new FakeSpecialistRepository());

        var result = await service.RegisterAsync(new RegisterSpecialistRequest(
            "Jane Doe",
            email,
            password,
            confirmPassword,
            "+15550101010"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Validation, result.Status);
        Assert.Equal(expectedCode, result.Error?.Code);
    }

    [Fact]
    public async Task LoginAsync_ReturnsUnauthorizedForWrongPassword()
    {
        var repository = new FakeSpecialistRepository();
        repository.Specialists.Add(new Specialist("Jane Doe", "jane@example.com", "+15550101010", "hash:Password1"));
        var service = CreateService(repository);

        var result = await service.LoginAsync(new LoginRequest("jane@example.com", "wrong"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task LoginAsync_IssuesTokensForValidCredentials()
    {
        var repository = new FakeSpecialistRepository();
        repository.Specialists.Add(new Specialist("Jane Doe", "jane@example.com", "+15550101010", "hash:Password1"));
        var service = CreateService(repository);

        var result = await service.LoginAsync(new LoginRequest("jane@example.com", "Password1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value?.AccessToken);
        Assert.Equal("hash:refresh-token", repository.Specialists[0].RefreshTokenHash);
    }

    [Fact]
    public async Task RefreshAsync_RotatesActiveRefreshToken()
    {
        var repository = new FakeSpecialistRepository();
        var specialist = new Specialist("Jane Doe", "jane@example.com", "+15550101010", "hash:Password1");
        specialist.SetRefreshToken("hash:old-refresh", new DateTimeOffset(2026, 6, 12, 0, 0, 0, TimeSpan.Zero));
        repository.Specialists.Add(specialist);
        var service = CreateService(repository);

        var result = await service.RefreshAsync(
            new RefreshTokenRequest(specialist.Id, "old-refresh"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("refresh-token", result.Value?.RefreshToken);
        Assert.Equal("hash:refresh-token", specialist.RefreshTokenHash);
    }

    [Fact]
    public async Task RefreshAsync_RejectsExpiredRefreshToken()
    {
        var repository = new FakeSpecialistRepository();
        var specialist = new Specialist("Jane Doe", "jane@example.com", "+15550101010", "hash:Password1");
        specialist.SetRefreshToken("hash:old-refresh", new DateTimeOffset(2026, 6, 10, 0, 0, 0, TimeSpan.Zero));
        repository.Specialists.Add(specialist);
        var service = CreateService(repository);

        var result = await service.RefreshAsync(
            new RefreshTokenRequest(specialist.Id, "old-refresh"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task GetMeAsync_ReturnsNotFoundForMissingSpecialist()
    {
        var service = CreateService(new FakeSpecialistRepository());

        var result = await service.GetMeAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    private static AuthService CreateService(FakeSpecialistRepository repository)
    {
        return new AuthService(
            repository,
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeDateTimeProvider(new DateTimeOffset(2026, 6, 11, 0, 0, 0, TimeSpan.Zero)));
    }

    private sealed class FakeSpecialistRepository : ISpecialistRepository
    {
        public List<Specialist> Specialists { get; } = [];

        public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            return Task.FromResult(Specialists.Any(specialist => specialist.Email == normalizedEmail));
        }

        public Task<Specialist?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            return Task.FromResult(Specialists.SingleOrDefault(specialist => specialist.Email == normalizedEmail));
        }

        public Task<Specialist?> GetByIdAsync(Guid specialistId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Specialists.SingleOrDefault(specialist => specialist.Id == specialistId));
        }

        public Task AddAsync(Specialist specialist, CancellationToken cancellationToken)
        {
            Specialists.Add(specialist);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
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
            return new AccessTokenResult("access-token", utcNow.AddMinutes(30));
        }

        public RefreshTokenResult CreateRefreshToken(DateTimeOffset utcNow)
        {
            return new RefreshTokenResult("refresh-token", utcNow.AddDays(30));
        }
    }

    private sealed class FakeDateTimeProvider(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
