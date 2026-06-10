namespace ServiceBooking.Application.Slots;

public interface ISlotRepository
{
    Task<bool> SpecialistExistsAsync(Guid specialistId, CancellationToken cancellationToken);

    Task<bool> IsVacationDateAsync(Guid specialistId, DateOnly date, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SlotBookingSnapshot>> GetConfirmedBookingsAsync(
        Guid specialistId,
        DateOnly date,
        CancellationToken cancellationToken);
}
