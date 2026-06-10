using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.SpecialistBookings;

public interface IBookingActionService
{
    Task<ServiceResult<PagedBookingResponse>> ListAsync(
        Guid specialistId,
        BookingListQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<SpecialistBookingResponse>> GetByIdAsync(
        Guid specialistId,
        Guid bookingId,
        CancellationToken cancellationToken);

    Task<ServiceResult<SpecialistBookingResponse>> ConfirmAsync(
        Guid specialistId,
        Guid bookingId,
        ConfirmBookingRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<SpecialistBookingResponse>> RejectAsync(
        Guid specialistId,
        Guid bookingId,
        RejectBookingRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<SpecialistBookingResponse>> CompleteAsync(
        Guid specialistId,
        Guid bookingId,
        CompleteBookingRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<SpecialistBookingResponse>> ReplyAsync(
        Guid specialistId,
        Guid bookingId,
        ReplyBookingRequest request,
        CancellationToken cancellationToken);
}
