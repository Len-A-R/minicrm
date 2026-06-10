using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Profile;

public interface IProfileService
{
    Task<ServiceResult<ProfileResponse>> GetProfileAsync(Guid specialistId, CancellationToken cancellationToken);

    Task<ServiceResult<ProfileResponse>> UpdateProfileAsync(
        Guid specialistId,
        UpdateSpecialistProfileRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProfileResponse>> UploadAvatarAsync(
        Guid specialistId,
        AvatarUploadRequest request,
        CancellationToken cancellationToken);
}
