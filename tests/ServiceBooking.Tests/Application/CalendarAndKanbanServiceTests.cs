using ServiceBooking.Application.Calendar;
using ServiceBooking.Application.Common;
using ServiceBooking.Application.Kanban;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Tests.Application;

public sealed class CalendarAndKanbanServiceTests
{
    [Fact]
    public async Task CalendarListAsync_ReturnsScheduledBookingsInRange()
    {
        var specialistId = Guid.NewGuid();
        var confirmed = CreateBooking(specialistId, new DateOnly(2026, 7, 12), new TimeOnly(10, 0));
        confirmed.Confirm(new DateOnly(2026, 7, 12), new TimeOnly(10, 0));
        var completed = CreateBooking(specialistId, new DateOnly(2026, 7, 13), new TimeOnly(11, 0));
        completed.Confirm(new DateOnly(2026, 7, 13), new TimeOnly(11, 0));
        completed.Complete(100m);
        var repository = new FakeSchedulingRepository { Bookings = { confirmed, completed } };
        var service = new CalendarService(repository);

        var result = await service.ListAsync(
            specialistId,
            new CalendarRangeQuery(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains(result.Value, item => item.Status == BookingStatus.Confirmed);
        Assert.Contains(result.Value, item => item.Status == BookingStatus.Completed);
    }

    [Fact]
    public async Task CalendarRescheduleAsync_ReturnsConflictForOverlappingBooking()
    {
        var specialistId = Guid.NewGuid();
        var target = CreateBooking(specialistId, new DateOnly(2026, 7, 12), new TimeOnly(10, 0));
        target.Confirm(new DateOnly(2026, 7, 12), new TimeOnly(10, 0));
        var existing = CreateBooking(specialistId, new DateOnly(2026, 7, 12), new TimeOnly(10, 30));
        existing.Confirm(new DateOnly(2026, 7, 12), new TimeOnly(10, 30));
        var repository = new FakeSchedulingRepository { Bookings = { target, existing } };
        var service = new CalendarService(repository);

        var result = await service.RescheduleAsync(
            specialistId,
            target.Id,
            new RescheduleBookingRequest(new DateOnly(2026, 7, 12), new TimeOnly(10, 15)),
            CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("slot_conflict", result.Error?.Code);
    }

    [Fact]
    public async Task CalendarCancelAsync_RejectsConfirmedBooking()
    {
        var specialistId = Guid.NewGuid();
        var booking = CreateBooking(specialistId, new DateOnly(2026, 7, 12), new TimeOnly(10, 0));
        booking.Confirm(new DateOnly(2026, 7, 12), new TimeOnly(10, 0));
        var repository = new FakeSchedulingRepository { Bookings = { booking } };
        var service = new CalendarService(repository);

        var result = await service.CancelAsync(specialistId, booking.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task KanbanGetBoardAsync_GroupsBookingsByStatus()
    {
        var specialistId = Guid.NewGuid();
        var newBooking = CreateBooking(specialistId, new DateOnly(2026, 7, 12), new TimeOnly(10, 0));
        var confirmed = CreateBooking(specialistId, new DateOnly(2026, 7, 12), new TimeOnly(12, 0));
        confirmed.Confirm(new DateOnly(2026, 7, 12), new TimeOnly(12, 0));
        var repository = new FakeSchedulingRepository { Bookings = { newBooking, confirmed } };
        var service = new KanbanService(repository);

        var result = await service.GetBoardAsync(
            specialistId,
            new KanbanBoardQuery(new DateOnly(2026, 7, 12)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.Columns.Count);
        Assert.Single(result.Value.Columns.Single(column => column.Status == BookingStatus.New).Items);
        Assert.Single(result.Value.Columns.Single(column => column.Status == BookingStatus.Confirmed).Items);
    }

    [Fact]
    public async Task KanbanMoveAsync_ConfirmsAndCompletesBooking()
    {
        var specialistId = Guid.NewGuid();
        var booking = CreateBooking(specialistId, new DateOnly(2026, 7, 12), new TimeOnly(10, 0));
        var repository = new FakeSchedulingRepository { Bookings = { booking } };
        var service = new KanbanService(repository);

        var confirmed = await service.MoveAsync(
            specialistId,
            booking.Id,
            new MoveKanbanBookingRequest(BookingStatus.Confirmed),
            CancellationToken.None);
        var completed = await service.MoveAsync(
            specialistId,
            booking.Id,
            new MoveKanbanBookingRequest(BookingStatus.Completed),
            CancellationToken.None);

        Assert.True(confirmed.IsSuccess);
        Assert.True(completed.IsSuccess);
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Equal(100m, booking.ActualRevenue);
        Assert.Equal(BookingStatus.Completed, booking.Status);
    }

    private static Booking CreateBooking(Guid specialistId, DateOnly date, TimeOnly time)
    {
        return new Booking(
            "Alice Brown",
            "+15550909090",
            specialistId,
            date,
            time,
            [new BookingService(Guid.NewGuid(), "Consultation", 100m, 60)]);
    }

    private sealed class FakeSchedulingRepository : ICalendarRepository, IKanbanRepository
    {
        public List<Booking> Bookings { get; } = [];
        public int SaveCount { get; private set; }

        public Task<IReadOnlyCollection<Booking>> ListScheduledAsync(
            Guid specialistId,
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Booking>>(Bookings
                .Where(booking => booking.SpecialistId == specialistId
                    && (booking.Status == BookingStatus.Confirmed || booking.Status == BookingStatus.Completed)
                    && (booking.ConfirmedDate ?? booking.RequestedDate) >= from
                    && (booking.ConfirmedDate ?? booking.RequestedDate) <= to)
                .ToArray());
        }

        public Task<IReadOnlyCollection<Booking>> ListByDateAsync(
            Guid specialistId,
            DateOnly date,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Booking>>(Bookings
                .Where(booking => booking.SpecialistId == specialistId
                    && (booking.RequestedDate == date || booking.ConfirmedDate == date))
                .ToArray());
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
                    && booking.Status == BookingStatus.Confirmed
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
