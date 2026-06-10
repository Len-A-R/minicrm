using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Tests.Domain;

public sealed class BookingTests
{
    [Fact]
    public void Constructor_CalculatesTotalsAndSetsNewStatus()
    {
        var specialistId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var bookingServices = new[]
        {
            new BookingService(serviceId, "Haircut", 35.50m, 45),
            new BookingService(Guid.NewGuid(), "Styling", 20m, 30)
        };

        var booking = new Booking(
            "Anna Smith",
            "+15551234567",
            specialistId,
            new DateOnly(2026, 6, 11),
            new TimeOnly(10, 30),
            bookingServices,
            "Window seat");

        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(BookingStatus.New, booking.Status);
        Assert.Equal(55.50m, booking.TotalPrice);
        Assert.Equal(75, booking.TotalDuration);
        Assert.Equal("Window seat", booking.Message);
    }

    [Fact]
    public void Constructor_RejectsInvalidClientName()
    {
        var services = new[] { new BookingService(Guid.NewGuid(), "Consultation", 100m, 60) };

        Assert.Throws<ArgumentException>(() => new Booking(
            "A",
            "+15551234567",
            Guid.NewGuid(),
            new DateOnly(2026, 6, 11),
            new TimeOnly(9, 0),
            services));
    }

    [Fact]
    public void Confirm_CompleteAndReject_UpdateStatusFields()
    {
        var booking = CreateBooking();

        booking.Confirm(new DateOnly(2026, 6, 12), new TimeOnly(14, 0));

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ConfirmedAt);
        Assert.Equal(new DateOnly(2026, 6, 12), booking.ConfirmedDate);
        Assert.Equal(new TimeOnly(14, 0), booking.ConfirmedTime);

        booking.Complete(120.25m);

        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Equal(120.25m, booking.ActualRevenue);
        Assert.NotNull(booking.CompletedAt);

        booking.Reject();

        Assert.Equal(BookingStatus.Rejected, booking.Status);
    }

    [Fact]
    public void SetMessage_RejectsMessagesLongerThanLimit()
    {
        var booking = CreateBooking();
        var longMessage = new string('x', 501);

        Assert.Throws<ArgumentException>(() => booking.SetMessage(longMessage));
    }

    private static Booking CreateBooking()
    {
        return new Booking(
            "Anna Smith",
            "+15551234567",
            Guid.NewGuid(),
            new DateOnly(2026, 6, 11),
            new TimeOnly(10, 30),
            [new BookingService(Guid.NewGuid(), "Haircut", 50m, 45)]);
    }
}
