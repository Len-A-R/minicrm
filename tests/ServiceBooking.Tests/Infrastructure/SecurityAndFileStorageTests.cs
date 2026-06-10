using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using ServiceBooking.Infrastructure.Files;
using ServiceBooking.Infrastructure.Security;

namespace ServiceBooking.Tests.Infrastructure;

public sealed class SecurityAndFileStorageTests
{
    [Fact]
    public void BCryptPasswordHasher_HashesAndVerifiesPasswords()
    {
        var hasher = new BCryptPasswordHasher();

        var hash = hasher.Hash("Password1");

        Assert.NotEqual("Password1", hash);
        Assert.True(hasher.Verify("Password1", hash));
        Assert.False(hasher.Verify("wrong", hash));
        Assert.False(hasher.Verify("", hash));
        Assert.False(hasher.Verify("Password1", ""));
    }

    [Theory]
    [InlineData("avatar.png", "image/png", ".png")]
    [InlineData("", "image/webp", ".webp")]
    public async Task LocalAvatarStorage_SavesAvatarAndReturnsPublicUrl(
        string fileName,
        string contentType,
        string expectedExtension)
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), "ServiceBookingTests", Guid.NewGuid().ToString("N"));
        var environment = new FakeHostEnvironment(contentRoot);
        var storage = new LocalAvatarStorage(environment);

        try
        {
            await using var stream = new MemoryStream([1, 2, 3, 4]);

            var url = await storage.SaveAvatarAsync(
                Guid.NewGuid(),
                stream,
                fileName,
                contentType,
                CancellationToken.None);

            Assert.StartsWith("/uploads/avatars/", url, StringComparison.Ordinal);
            Assert.EndsWith(expectedExtension, url, StringComparison.Ordinal);

            var storedPath = Path.Combine(
                contentRoot,
                "wwwroot",
                url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(storedPath));
        }
        finally
        {
            if (Directory.Exists(contentRoot))
            {
                Directory.Delete(contentRoot, recursive: true);
            }
        }
    }

    private sealed class FakeHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "ServiceBooking.Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
