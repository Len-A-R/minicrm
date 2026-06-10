using ServiceBooking.Application.Common;
using ServiceBooking.Application.Specialists;
using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Application.Profile;

public sealed class ProfileService(
    ISpecialistRepository specialists,
    IAvatarStorage avatarStorage) : IProfileService
{
    private const long MaxAvatarBytes = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedAvatarContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public async Task<ServiceResult<ProfileResponse>> GetProfileAsync(
        Guid specialistId,
        CancellationToken cancellationToken)
    {
        var specialist = await specialists.GetByIdAsync(specialistId, cancellationToken);
        return specialist is null
            ? NotFound()
            : ServiceResult<ProfileResponse>.Success(ToResponse(specialist));
    }

    public async Task<ServiceResult<ProfileResponse>> UpdateProfileAsync(
        Guid specialistId,
        UpdateSpecialistProfileRequest request,
        CancellationToken cancellationToken)
    {
        var specialist = await specialists.GetByIdAsync(specialistId, cancellationToken);
        if (specialist is null)
        {
            return NotFound();
        }

        if (request.LocationId == Guid.Empty)
        {
            return Validation("invalid_location", "Location id must be null or a non-empty UUID.");
        }

        try
        {
            specialist.UpdateProfile(request.FullName, request.Phone, request.VenueName, request.LocationId);
        }
        catch (ArgumentException exception)
        {
            return Validation("invalid_profile", exception.Message);
        }

        await specialists.SaveChangesAsync(cancellationToken);
        return ServiceResult<ProfileResponse>.Success(ToResponse(specialist));
    }

    public async Task<ServiceResult<ProfileResponse>> UploadAvatarAsync(
        Guid specialistId,
        AvatarUploadRequest request,
        CancellationToken cancellationToken)
    {
        var specialist = await specialists.GetByIdAsync(specialistId, cancellationToken);
        if (specialist is null)
        {
            return NotFound();
        }

        if (request.Length <= 0 || request.Length > MaxAvatarBytes)
        {
            return Validation("invalid_avatar_size", "Avatar file must be between 1 byte and 2 MB.");
        }

        if (!AllowedAvatarContentTypes.Contains(request.ContentType))
        {
            return Validation("invalid_avatar_type", "Avatar must be JPEG, PNG or WebP.");
        }

        var avatarUrl = await avatarStorage.SaveAvatarAsync(
            specialist.Id,
            request.Content,
            request.FileName,
            request.ContentType,
            cancellationToken);

        specialist.SetAvatarUrl(avatarUrl);
        await specialists.SaveChangesAsync(cancellationToken);

        return ServiceResult<ProfileResponse>.Success(ToResponse(specialist));
    }

    private static ServiceResult<ProfileResponse> NotFound()
    {
        return ServiceResult<ProfileResponse>.Failure(
            ResultStatus.NotFound,
            "specialist_not_found",
            "Specialist was not found.");
    }

    private static ServiceResult<ProfileResponse> Validation(string code, string message)
    {
        return ServiceResult<ProfileResponse>.Failure(ResultStatus.Validation, code, message);
    }

    private static ProfileResponse ToResponse(Specialist specialist)
    {
        return new ProfileResponse(
            specialist.Id,
            specialist.FullName,
            specialist.Email,
            specialist.Phone,
            specialist.AvatarUrl,
            specialist.VenueName,
            specialist.LocationId,
            specialist.CreatedAt);
    }
}
