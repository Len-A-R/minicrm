using ServiceBooking.Application.Bookings;
using ServiceBooking.Application.Common;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Application.SpecialistBookings;

public sealed class BookingActionService(IBookingActionRepository bookings) : IBookingActionService
{
    public async Task<ServiceResult<PagedBookingResponse>> ListAsync(
        Guid specialistId,
        BookingListQuery query,
        CancellationToken cancellationToken)
    {
        if (specialistId == Guid.Empty)
        {
            return Validation<PagedBookingResponse>("invalid_specialist_id", "Specialist id is required.");
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var status = ParseStatus(query.Status);
        if (query.Status is not null && status is null)
        {
            return Validation<PagedBookingResponse>("invalid_status", "Booking status is invalid.");
        }

        var (items, totalCount) = await bookings.ListAsync(
            specialistId,
            status,
            query.Date,
            query.Search,
            page,
            pageSize,
            cancellationToken);

        return ServiceResult<PagedBookingResponse>.Success(new PagedBookingResponse(
            items.Select(ToResponse).ToArray(),
            page,
            pageSize,
            totalCount));
    }

    public async Task<ServiceResult<SpecialistBookingResponse>> GetByIdAsync(
        Guid specialistId,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var booking = await GetBookingOrFailureAsync(specialistId, bookingId, cancellationToken);
        return booking.IsSuccess
            ? ServiceResult<SpecialistBookingResponse>.Success(ToResponse(booking.Value!))
            : ServiceResult<SpecialistBookingResponse>.Failure(booking.Status, booking.Error!.Code, booking.Error.Message);
    }

    public async Task<ServiceResult<SpecialistBookingResponse>> ConfirmAsync(
        Guid specialistId,
        Guid bookingId,
        ConfirmBookingRequest request,
        CancellationToken cancellationToken)
    {
        var bookingResult = await GetBookingOrFailureAsync(specialistId, bookingId, cancellationToken);
        if (!bookingResult.IsSuccess)
        {
            return FailureFromBookingResult(bookingResult);
        }

        var booking = bookingResult.Value!;
        if (booking.Status is BookingStatus.Rejected or BookingStatus.Completed)
        {
            return ServiceResult<SpecialistBookingResponse>.Failure(
                ResultStatus.Conflict,
                "invalid_booking_status",
                "Rejected or completed booking cannot be confirmed.");
        }

        var conflicts = await bookings.GetBookingsForConflictCheckAsync(
            specialistId,
            request.Date,
            bookingId,
            cancellationToken);

        if (HasConflict(conflicts, request.Time, BookingDuration(booking)))
        {
            return ServiceResult<SpecialistBookingResponse>.Failure(
                ResultStatus.Conflict,
                "slot_conflict",
                "Selected date and time conflict with another booking.");
        }

        booking.Confirm(request.Date, request.Time);
        await bookings.SaveChangesAsync(cancellationToken);
        return ServiceResult<SpecialistBookingResponse>.Success(ToResponse(booking));
    }

    public async Task<ServiceResult<SpecialistBookingResponse>> RejectAsync(
        Guid specialistId,
        Guid bookingId,
        RejectBookingRequest request,
        CancellationToken cancellationToken)
    {
        var bookingResult = await GetBookingOrFailureAsync(specialistId, bookingId, cancellationToken);
        if (!bookingResult.IsSuccess)
        {
            return FailureFromBookingResult(bookingResult);
        }

        try
        {
            bookingResult.Value!.Reject(request.Reason);
            await bookings.SaveChangesAsync(cancellationToken);
            return ServiceResult<SpecialistBookingResponse>.Success(ToResponse(bookingResult.Value));
        }
        catch (ArgumentException exception)
        {
            return Validation<SpecialistBookingResponse>("invalid_rejection_reason", exception.Message);
        }
    }

    public async Task<ServiceResult<SpecialistBookingResponse>> CompleteAsync(
        Guid specialistId,
        Guid bookingId,
        CompleteBookingRequest request,
        CancellationToken cancellationToken)
    {
        var bookingResult = await GetBookingOrFailureAsync(specialistId, bookingId, cancellationToken);
        if (!bookingResult.IsSuccess)
        {
            return FailureFromBookingResult(bookingResult);
        }

        if (request.ActualRevenue < 0)
        {
            return Validation<SpecialistBookingResponse>("invalid_actual_revenue", "Actual revenue cannot be negative.");
        }

        bookingResult.Value!.Complete(request.ActualRevenue);
        await bookings.SaveChangesAsync(cancellationToken);
        return ServiceResult<SpecialistBookingResponse>.Success(ToResponse(bookingResult.Value));
    }

    public async Task<ServiceResult<SpecialistBookingResponse>> ReplyAsync(
        Guid specialistId,
        Guid bookingId,
        ReplyBookingRequest request,
        CancellationToken cancellationToken)
    {
        var bookingResult = await GetBookingOrFailureAsync(specialistId, bookingId, cancellationToken);
        if (!bookingResult.IsSuccess)
        {
            return FailureFromBookingResult(bookingResult);
        }

        try
        {
            bookingResult.Value!.Reply(request.Reply);
            await bookings.SaveChangesAsync(cancellationToken);
            return ServiceResult<SpecialistBookingResponse>.Success(ToResponse(bookingResult.Value));
        }
        catch (ArgumentException exception)
        {
            return Validation<SpecialistBookingResponse>("invalid_reply", exception.Message);
        }
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

        var booking = await bookings.GetByIdAsync(specialistId, bookingId, cancellationToken);
        return booking is null
            ? ServiceResult<Booking>.Failure(ResultStatus.NotFound, "booking_not_found", "Booking was not found.")
            : ServiceResult<Booking>.Success(booking);
    }

    private static bool HasConflict(IEnumerable<Booking> confirmedBookings, TimeOnly start, int durationMinutes)
    {
        var end = start.AddMinutes(durationMinutes);
        return confirmedBookings.Any(existing =>
        {
            var existingStart = existing.ConfirmedTime ?? existing.RequestedTime;
            var existingEnd = existingStart.AddMinutes(BookingDuration(existing));
            return start < existingEnd && existingStart < end;
        });
    }

    private static int BookingDuration(Booking booking) => Math.Max(booking.TotalDuration, 30);

    private static BookingStatus? ParseStatus(string? status)
    {
        return Enum.TryParse<BookingStatus>(status, ignoreCase: true, out var parsed) ? parsed : null;
    }

    private static ServiceResult<T> Validation<T>(string code, string message)
    {
        return ServiceResult<T>.Failure(ResultStatus.Validation, code, message);
    }

    private static ServiceResult<SpecialistBookingResponse> FailureFromBookingResult(ServiceResult<Booking> result)
    {
        return ServiceResult<SpecialistBookingResponse>.Failure(
            result.Status,
            result.Error!.Code,
            result.Error.Message);
    }

    private static SpecialistBookingResponse ToResponse(Booking booking)
    {
        return new SpecialistBookingResponse(
            booking.Id,
            booking.ClientName,
            booking.ClientPhone,
            booking.SpecialistId,
            booking.ClientId,
            booking.Services
                .Select(service => new BookingServiceItemResponse(
                    service.ServiceId,
                    service.ServiceName,
                    service.Price,
                    service.DurationMinutes))
                .ToArray(),
            booking.RequestedDate,
            booking.RequestedTime,
            booking.Message,
            booking.TotalPrice,
            booking.TotalDuration,
            booking.Status,
            booking.CreatedAt,
            booking.ConfirmedAt,
            booking.ConfirmedDate,
            booking.ConfirmedTime,
            booking.CompletedAt,
            booking.ActualRevenue,
            booking.RejectionReason,
            booking.SpecialistReply,
            booking.RepliedAt);
    }
}
