using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Slots;

public sealed class SlotService(ISlotRepository repository) : ISlotService
{
    private static readonly TimeOnly WorkdayStart = new(9, 0);
    private static readonly TimeOnly WorkdayEnd = new(18, 0);
    private const int StepMinutes = 30;

    public async Task<ServiceResult<IReadOnlyCollection<AvailableSlotResponse>>> GetAvailableSlotsAsync(
        Guid specialistId,
        DateOnly date,
        int durationMinutes,
        CancellationToken cancellationToken)
    {
        if (specialistId == Guid.Empty)
        {
            return Validation("invalid_specialist_id", "Specialist id must be a non-empty UUID.");
        }

        if (durationMinutes <= 0)
        {
            return Validation("invalid_duration", "Duration must be greater than zero.");
        }

        if (!await repository.SpecialistExistsAsync(specialistId, cancellationToken))
        {
            return ServiceResult<IReadOnlyCollection<AvailableSlotResponse>>.Failure(
                ResultStatus.NotFound,
                "specialist_not_found",
                "Specialist was not found.");
        }

        if (await repository.IsVacationDateAsync(specialistId, date, cancellationToken))
        {
            return ServiceResult<IReadOnlyCollection<AvailableSlotResponse>>.Success([]);
        }

        var bookings = await repository.GetConfirmedBookingsAsync(specialistId, date, cancellationToken);
        var slots = new List<AvailableSlotResponse>();
        var candidate = WorkdayStart;

        while (candidate.AddMinutes(durationMinutes) <= WorkdayEnd)
        {
            if (!bookings.Any(booking => Conflicts(candidate, durationMinutes, booking)))
            {
                slots.Add(new AvailableSlotResponse(date, candidate, durationMinutes));
            }

            candidate = candidate.AddMinutes(StepMinutes);
        }

        return ServiceResult<IReadOnlyCollection<AvailableSlotResponse>>.Success(slots);
    }

    private static bool Conflicts(TimeOnly candidateStart, int candidateDuration, SlotBookingSnapshot booking)
    {
        var candidateEnd = candidateStart.AddMinutes(candidateDuration);
        var bookingEnd = booking.Time.AddMinutes(Math.Max(booking.DurationMinutes, StepMinutes));

        return candidateStart < bookingEnd && booking.Time < candidateEnd;
    }

    private static ServiceResult<IReadOnlyCollection<AvailableSlotResponse>> Validation(string code, string message)
    {
        return ServiceResult<IReadOnlyCollection<AvailableSlotResponse>>.Failure(ResultStatus.Validation, code, message);
    }
}
