namespace ServiceBooking.Application.Profile;

public sealed record AvatarUploadRequest(
    Stream Content,
    string FileName,
    string ContentType,
    long Length);
