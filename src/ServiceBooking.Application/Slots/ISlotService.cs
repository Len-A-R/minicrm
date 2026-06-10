using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Slots;

public interface ISlotService
{
    Task<ServiceResult<IReadOnlyCollection<AvailableSlotResponse>>> GetAvailableSlotsAsync(
        Guid specialistId,
        DateOnly date,
        int durationMinutes,
        CancellationToken cancellationToken);
}
