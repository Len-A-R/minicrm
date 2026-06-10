using ServiceBooking.Application.Common;
using ServiceBooking.Application.Profile;
using ServiceBooking.Application.Specialists;
using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Tests.Application;

public sealed class ProfileServiceTests
{
    [Fact]
    public async Task GetProfileAsync_ReturnsProfile()
    {
        var repository = new FakeSpecialistRepository();
        var specialist = new Specialist("Jane Doe", "jane@example.com", "+15550101010", "hash:Password1");
        repository.Specialists.Add(specialist);
        var service = new ProfileService(repository, new FakeAvatarStorage());

        var result = await service.GetProfileAsync(specialist.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Jane Doe", result.Value?.FullName);
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesEditableFields()
    {
        var repository = new FakeSpecialistRepository();
        var specialist = new Specialist("Jane Doe", "jane@example.com", "+15550101010", "hash:Password1");
        repository.Specialists.Add(specialist);
        var service = new ProfileService(repository, new FakeAvatarStorage());

        var locationId = Guid.NewGuid();
        var result = await service.UpdateProfileAsync(
            specialist.Id,
            new UpdateSpecialistProfileRequest("Jane Smith", "+15550202020", "Main Studio", locationId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Jane Smith", result.Value?.FullName);
        Assert.Equal("Main Studio", result.Value?.VenueName);
        Assert.Equal(locationId, result.Value?.LocationId);
    }

    [Fact]
    public async Task UpdateProfileAsync_ReturnsValidationForInvalidProfile()
    {
        var repository = new FakeSpecialistRepository();
        var specialist = new Specialist("Jane Doe", "jane@example.com", "+15550101010", "hash:Password1");
        repository.Specialists.Add(specialist);
        var service = new ProfileService(repository, new FakeAvatarStorage());

        var result = await service.UpdateProfileAsync(
            specialist.Id,
            new UpdateSpecialistProfileRequest("J", "+15550202020", null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Validation, result.Status);
    }

    [Fact]
    public async Task UpdateProfileAsync_ReturnsNotFoundForMissingSpecialist()
    {
        var service = new ProfileService(new FakeSpecialistRepository(), new FakeAvatarStorage());

        var result = await service.UpdateProfileAsync(
            Guid.NewGuid(),
            new UpdateSpecialistProfileRequest("Jane Smith", "+15550202020", null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UpdateProfileAsync_RejectsEmptyLocationId()
    {
        var repository = new FakeSpecialistRepository();
        var specialist = new Specialist("Jane Doe", "jane@example.com", "+15550101010", "hash:Password1");
        repository.Specialists.Add(specialist);
        var service = new ProfileService(repository, new FakeAvatarStorage());

        var result = await service.UpdateProfileAsync(
            specialist.Id,
            new UpdateSpecialistProfileRequest("Jane Smith", "+15550202020", null, Guid.Empty),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid_location", result.Error?.Code);
    }

    [Fact]
    public async Task UploadAvatarAsync_StoresAvatarUrl()
    {
        var repository = new FakeSpecialistRepository();
        var specialist = new Specialist("Jane Doe", "jane@example.com", "+15550101010", "hash:Password1");
        repository.Specialists.Add(specialist);
        var service = new ProfileService(repository, new FakeAvatarStorage());
        await using var stream = new MemoryStream([1, 2, 3]);

        var result = await service.UploadAvatarAsync(
            specialist.Id,
            new AvatarUploadRequest(stream, "avatar.png", "image/png", stream.Length),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("/test/avatar.png", result.Value?.AvatarUrl);
        Assert.Equal("/test/avatar.png", specialist.AvatarUrl);
    }

    [Theory]
    [InlineData("text/plain", 10, "invalid_avatar_type")]
    [InlineData("image/png", 0, "invalid_avatar_size")]
    public async Task UploadAvatarAsync_ValidatesAvatar(string contentType, long length, string expectedCode)
    {
        var repository = new FakeSpecialistRepository();
        var specialist = new Specialist("Jane Doe", "jane@example.com", "+15550101010", "hash:Password1");
        repository.Specialists.Add(specialist);
        var service = new ProfileService(repository, new FakeAvatarStorage());
        await using var stream = new MemoryStream([1, 2, 3]);

        var result = await service.UploadAvatarAsync(
            specialist.Id,
            new AvatarUploadRequest(stream, "avatar.txt", contentType, length),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Validation, result.Status);
        Assert.Equal(expectedCode, result.Error?.Code);
    }

    [Fact]
    public async Task UploadAvatarAsync_ReturnsNotFoundForMissingSpecialist()
    {
        var service = new ProfileService(new FakeSpecialistRepository(), new FakeAvatarStorage());
        await using var stream = new MemoryStream([1, 2, 3]);

        var result = await service.UploadAvatarAsync(
            Guid.NewGuid(),
            new AvatarUploadRequest(stream, "avatar.png", "image/png", stream.Length),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsNotFoundForMissingSpecialist()
    {
        var service = new ProfileService(new FakeSpecialistRepository(), new FakeAvatarStorage());

        var result = await service.GetProfileAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
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

    private sealed class FakeAvatarStorage : IAvatarStorage
    {
        public Task<string> SaveAvatarAsync(
            Guid specialistId,
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            return Task.FromResult($"/test/{fileName}");
        }
    }
}
