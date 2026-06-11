using ServiceBooking.Application.Common;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Application.Kanban;

public sealed class KanbanService(IKanbanRepository kanbanRepository) : IKanbanService
{
    private static readonly BookingStatus[] Columns =
    [
        BookingStatus.New,
        BookingStatus.Confirmed,
        BookingStatus.Rejected,
        BookingStatus.Completed
    ];

    public async Task<ServiceResult<KanbanBoardResponse>> GetBoardAsync(
        Guid specialistId,
        KanbanBoardQuery query,
        CancellationToken cancellationToken)
    {
        if (specialistId == Guid.Empty)
        {
            return Validation<KanbanBoardResponse>("invalid_specialist_id", "Specialist id is required.");
        }

        var bookings = await kanbanRepository.ListByDateAsync(specialistId, query.Date, cancellationToken);
        var columns = Columns
            .Select(status => new KanbanColumnResponse(
                status,
                bookings
                    .Where(booking => booking.Status == status)
                    .OrderBy(booking => ScheduledTime(booking))
                    .Select(ToCard)
                    .ToArray()))
            .ToArray();

        return ServiceResult<KanbanBoardResponse>.Success(new KanbanBoardResponse(query.Date, columns));
    }

    public async Task<ServiceResult<KanbanBookingCardResponse>> MoveAsync(
        Guid specialistId,
        Guid bookingId,
        MoveKanbanBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (!Columns.Contains(request.Status))
        {
            return Validation<KanbanBookingCardResponse>("invalid_status", "Booking status is invalid.");
        }

        var bookingResult = await GetBookingOrFailureAsync(specialistId, bookingId, cancellationToken);
        if (!bookingResult.IsSuccess)
        {
            return ServiceResult<KanbanBookingCardResponse>.Failure(
                bookingResult.Status,
                bookingResult.Error!.Code,
                bookingResult.Error.Message);
        }

        var booking = bookingResult.Value!;
        if (request.Status == BookingStatus.Confirmed)
        {
            var date = booking.ConfirmedDate ?? booking.RequestedDate;
            var time = booking.ConfirmedTime ?? booking.RequestedTime;
            var conflicts = await kanbanRepository.GetBookingsForConflictCheckAsync(
                specialistId,
                date,
                bookingId,
                cancellationToken);

            if (HasConflict(conflicts, time, BookingDuration(booking)))
            {
                return ServiceResult<KanbanBookingCardResponse>.Failure(
                    ResultStatus.Conflict,
                    "slot_conflict",
                    "Booking conflicts with another confirmed booking.");
            }
        }

        ApplyStatus(booking, request.Status);
        await kanbanRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<KanbanBookingCardResponse>.Success(ToCard(booking));
    }

    private async Task<ServiceResult<Booking>> GetBookingOrFailureAsync(
        Guid specialistId,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        if (specialistId == Guid.Empty || bookingId == Guid.Empty)
        {
            return Validation<Booking>("invalid_booking_id", "Booking id is required.");
        }

        var booking = await kanbanRepository.GetByIdAsync(specialistId, bookingId, cancellationToken);
        return booking is null
            ? ServiceResult<Booking>.Failure(ResultStatus.NotFound, "booking_not_found", "Booking was not found.")
            : ServiceResult<Booking>.Success(booking);
    }

    private static void ApplyStatus(Booking booking, BookingStatus status)
    {
        switch (status)
        {
            case BookingStatus.New:
                booking.Reopen();
                break;
            case BookingStatus.Confirmed:
                booking.Confirm(booking.ConfirmedDate ?? booking.RequestedDate, booking.ConfirmedTime ?? booking.RequestedTime);
                break;
            case BookingStatus.Rejected:
                booking.Reject("Moved to rejected in Kanban.");
                break;
            case BookingStatus.Completed:
                booking.Complete(booking.ActualRevenue ?? booking.TotalPrice);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported booking status.");
        }
    }

    private static bool HasConflict(IEnumerable<Booking> scheduledBookings, TimeOnly start, int durationMinutes)
    {
        var end = start.AddMinutes(durationMinutes);
        return scheduledBookings.Any(existing =>
        {
            var existingStart = ScheduledTime(existing);
            var existingEnd = existingStart.AddMinutes(BookingDuration(existing));
            return start < existingEnd && existingStart < end;
        });
    }

    private static DateOnly ScheduledDate(Booking booking) => booking.ConfirmedDate ?? booking.RequestedDate;

    private static TimeOnly ScheduledTime(Booking booking) => booking.ConfirmedTime ?? booking.RequestedTime;

    private static int BookingDuration(Booking booking) => Math.Max(booking.TotalDuration, 30);

    private static KanbanBookingCardResponse ToCard(Booking booking)
    {
        return new KanbanBookingCardResponse(
            booking.Id,
            booking.ClientName,
            booking.ClientPhone,
            ScheduledDate(booking),
            ScheduledTime(booking),
            string.Join(", ", booking.Services.Select(service => service.ServiceName)),
            booking.TotalPrice,
            booking.TotalDuration,
            booking.Message);
    }

    private static ServiceResult<T> Validation<T>(string code, string message)
    {
        return ServiceResult<T>.Failure(ResultStatus.Validation, code, message);
    }
}
