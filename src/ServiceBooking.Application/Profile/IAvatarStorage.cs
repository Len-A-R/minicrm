namespace ServiceBooking.Application.Profile;

public interface IAvatarStorage
{
    Task<string> SaveAvatarAsync(
        Guid specialistId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);
}
