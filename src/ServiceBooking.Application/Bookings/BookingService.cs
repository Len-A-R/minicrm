using System.Text.RegularExpressions;
using ServiceBooking.Application.Common;
using DomainBooking = ServiceBooking.Domain.Entities.Booking;
using DomainBookingService = ServiceBooking.Domain.Entities.BookingService;

namespace ServiceBooking.Application.Bookings;

public sealed partial class BookingService(
    IBookingRepository bookings,
    IClientAutoCreationService clientAutoCreation,
    IDateTimeProvider dateTimeProvider) : IBookingService
{
    public async Task<ServiceResult<BookingResponse>> CreateAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateRequest(request);
        if (validation is not null)
        {
            return validation;
        }

        if (!await bookings.SpecialistExistsAsync(request.SpecialistId, cancellationToken))
        {
            return ServiceResult<BookingResponse>.Failure(
                ResultStatus.NotFound,
                "specialist_not_found",
                "Specialist was not found.");
        }

        var distinctServiceIds = request.ServiceIds
            .Where(serviceId => serviceId != Guid.Empty)
            .Distinct()
            .ToArray();
        var selectedServices = await bookings.GetSpecialistServicesAsync(
            request.SpecialistId,
            distinctServiceIds,
            cancellationToken);

        if (distinctServiceIds.Length > 0 && selectedServices.Count != distinctServiceIds.Length)
        {
            return ServiceResult<BookingResponse>.Failure(
                ResultStatus.Validation,
                "invalid_services",
                "One or more selected services are not provided by the specialist.");
        }

        var client = await clientAutoCreation.GetOrCreateAsync(
            request.ClientName,
            request.ClientPhone,
            cancellationToken);

        var bookingServices = selectedServices
            .Select(service => new DomainBookingService(
                service.ServiceId,
                service.ServiceName,
                service.Price,
                service.DurationMinutes))
            .ToArray();

        try
        {
            var booking = new DomainBooking(
                request.ClientName,
                request.ClientPhone,
                request.SpecialistId,
                request.RequestedDate,
                request.RequestedTime,
                bookingServices,
                request.Message,
                client.Id);

            await bookings.AddAsync(booking, cancellationToken);
            await bookings.SaveChangesAsync(cancellationToken);

            return ServiceResult<BookingResponse>.Success(ToResponse(booking));
        }
        catch (ArgumentException exception)
        {
            return ServiceResult<BookingResponse>.Failure(
                ResultStatus.Validation,
                "invalid_booking",
                exception.Message);
        }
    }

    private ServiceResult<BookingResponse>? ValidateRequest(CreateBookingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClientName) || !NameRegex().IsMatch(request.ClientName.Trim()))
        {
            return Validation("invalid_client_name", "Client name must contain at least 2 letters and spaces only.");
        }

        if (string.IsNullOrWhiteSpace(request.ClientPhone) || !PhoneRegex().IsMatch(request.ClientPhone.Trim()))
        {
            return Validation("invalid_client_phone", "Client phone format is invalid.");
        }

        if (request.SpecialistId == Guid.Empty)
        {
            return Validation("invalid_specialist_id", "Specialist id is required.");
        }

        if (request.RequestedDate < DateOnly.FromDateTime(dateTimeProvider.UtcNow.DateTime))
        {
            return Validation("invalid_requested_date", "Requested date cannot be in the past.");
        }

        if (request.Message is { Length: > 500 })
        {
            return Validation("invalid_message", "Message cannot exceed 500 characters.");
        }

        if ((request.ServiceIds is null || request.ServiceIds.Count == 0)
            && string.IsNullOrWhiteSpace(request.Message))
        {
            return Validation("empty_booking", "Select at least one service or provide a message.");
        }

        return null;
    }

    private static ServiceResult<BookingResponse> Validation(string code, string message)
    {
        return ServiceResult<BookingResponse>.Failure(ResultStatus.Validation, code, message);
    }

    private static BookingResponse ToResponse(DomainBooking booking)
    {
        return new BookingResponse(
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
            booking.CreatedAt);
    }

    [GeneratedRegex(@"^[\p{L} ]{2,100}$")]
    private static partial Regex NameRegex();

    [GeneratedRegex(@"^\+?[0-9][0-9\s().-]{6,30}$")]
    private static partial Regex PhoneRegex();
}
