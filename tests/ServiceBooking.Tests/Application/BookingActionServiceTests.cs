using ServiceBooking.Application.Common;
using ServiceBooking.Application.SpecialistBookings;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Tests.Application;

public sealed class BookingActionServiceTests
{
    [Fact]
    public async Task ConfirmAsync_ConfirmsBookingWhenSlotIsFree()
    {
        var specialistId = Guid.NewGuid();
        var booking = CreateBooking(specialistId, new TimeOnly(10, 0));
        var repository = new FakeBookingActionRepository { Bookings = { booking } };
        var service = new BookingActionService(repository);

        var result = await service.ConfirmAsync(
            specialistId,
            booking.Id,
            new ConfirmBookingRequest(new DateOnly(2026, 7, 12), new TimeOnly(11, 0)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Confirmed, result.Value?.Status);
        Assert.Equal(new TimeOnly(11, 0), result.Value?.ConfirmedTime);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task ConfirmAsync_ReturnsConflictForOverlappingConfirmedBooking()
    {
        var specialistId = Guid.NewGuid();
        var target = CreateBooking(specialistId, new TimeOnly(10, 0));
        var existing = CreateBooking(specialistId, new TimeOnly(10, 30));
        existing.Confirm(new DateOnly(2026, 7, 12), new TimeOnly(10, 30));
        var repository = new FakeBookingActionRepository { Bookings = { target, existing } };
        var service = new BookingActionService(repository);

        var result = await service.ConfirmAsync(
            specialistId,
            target.Id,
            new ConfirmBookingRequest(new DateOnly(2026, 7, 12), new TimeOnly(10, 0)),
            CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("slot_conflict", result.Error?.Code);
    }

    [Fact]
    public async Task RejectCompleteAndReply_UpdateBooking()
    {
        var specialistId = Guid.NewGuid();
        var booking = CreateBooking(specialistId, new TimeOnly(10, 0));
        var repository = new FakeBookingActionRepository { Bookings = { booking } };
        var service = new BookingActionService(repository);

        var reply = await service.ReplyAsync(specialistId, booking.Id, new ReplyBookingRequest("See you soon"), CancellationToken.None);
        var completed = await service.CompleteAsync(specialistId, booking.Id, new CompleteBookingRequest(125.50m), CancellationToken.None);
        var rejected = await service.RejectAsync(specialistId, booking.Id, new RejectBookingRequest("Client cancelled"), CancellationToken.None);

        Assert.True(reply.IsSuccess);
        Assert.Equal("See you soon", reply.Value?.SpecialistReply);
        Assert.Equal(BookingStatus.Completed, completed.Value?.Status);
        Assert.Equal(125.50m, completed.Value?.ActualRevenue);
        Assert.Equal(BookingStatus.Rejected, rejected.Value?.Status);
        Assert.Equal("Client cancelled", rejected.Value?.RejectionReason);
    }

    [Fact]
    public async Task ListAsync_AppliesStatusAndPagination()
    {
        var specialistId = Guid.NewGuid();
        var first = CreateBooking(specialistId, new TimeOnly(10, 0));
        var second = CreateBooking(specialistId, new TimeOnly(12, 0));
        second.Confirm(new DateOnly(2026, 7, 12), new TimeOnly(12, 0));
        var repository = new FakeBookingActionRepository { Bookings = { first, second } };
        var service = new BookingActionService(repository);

        var result = await service.ListAsync(
            specialistId,
            new BookingListQuery("Confirmed", null, null, 1, 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(BookingStatus.Confirmed, result.Value.Items.Single().Status);
    }

    private static Booking CreateBooking(Guid specialistId, TimeOnly requestedTime)
    {
        return new Booking(
            "Alice Brown",
            "+15550909090",
            specialistId,
            new DateOnly(2026, 7, 12),
            requestedTime,
            [new BookingService(Guid.NewGuid(), "Consultation", 100m, 60)]);
    }

    private sealed class FakeBookingActionRepository : IBookingActionRepository
    {
        public List<Booking> Bookings { get; } = [];
        public int SaveCount { get; private set; }

        public Task<(IReadOnlyCollection<Booking> Items, int TotalCount)> ListAsync(
            Guid specialistId,
            BookingStatus? status,
            DateOnly? date,
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var query = Bookings.Where(booking => booking.SpecialistId == specialistId);
            if (status.HasValue)
            {
                query = query.Where(booking => booking.Status == status.Value);
            }

            var filtered = query.ToArray();
            return Task.FromResult<(IReadOnlyCollection<Booking>, int)>(
                (filtered.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), filtered.Length));
        }

        public Task<Booking?> GetByIdAsync(Guid specialistId, Guid bookingId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Bookings.SingleOrDefault(booking => booking.SpecialistId == specialistId && booking.Id == bookingId));
        }

        public Task<IReadOnlyCollection<Booking>> GetBookingsForConflictCheckAsync(
            Guid specialistId,
            DateOnly date,
            Guid excludedBookingId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Booking>>(Bookings
                .Where(booking => booking.SpecialistId == specialistId
                    && booking.Id != excludedBookingId
                    && (booking.Status == BookingStatus.Confirmed || booking.Status == BookingStatus.Completed)
                    && (booking.ConfirmedDate ?? booking.RequestedDate) == date)
                .ToArray());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
