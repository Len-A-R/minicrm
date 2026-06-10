using Microsoft.Extensions.Hosting;
using ServiceBooking.Application.Profile;

namespace ServiceBooking.Infrastructure.Files;

public sealed class LocalAvatarStorage(IHostEnvironment environment) : IAvatarStorage
{
    public async Task<string> SaveAvatarAsync(
        Guid specialistId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var extension = GetExtension(fileName, contentType);
        var storedFileName = $"{specialistId:N}-{Guid.NewGuid():N}{extension}";
        var relativeDirectory = Path.Combine("uploads", "avatars");
        var absoluteDirectory = Path.Combine(environment.ContentRootPath, "wwwroot", relativeDirectory);

        Directory.CreateDirectory(absoluteDirectory);

        var absolutePath = Path.Combine(absoluteDirectory, storedFileName);
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await using var fileStream = File.Create(absolutePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return $"/uploads/avatars/{storedFileName}";
    }

    private static string GetExtension(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension.ToLowerInvariant();
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".bin"
        };
    }
}
