using ServiceBooking.Application.Common;
using ServiceBooking.Application.Slots;

namespace ServiceBooking.Tests.Application;

public sealed class SlotServiceTests
{
    [Fact]
    public async Task GetAvailableSlotsAsync_ReturnsWorkdaySlots()
    {
        var specialistId = Guid.NewGuid();
        var repository = new FakeSlotRepository { SpecialistIds = { specialistId } };
        var service = new SlotService(repository);

        var result = await service.GetAvailableSlotsAsync(
            specialistId,
            new DateOnly(2026, 7, 1),
            30,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(18, result.Value?.Count);
        Assert.Contains(result.Value!, slot => slot.Time == new TimeOnly(9, 0));
        Assert.Contains(result.Value!, slot => slot.Time == new TimeOnly(17, 30));
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ReturnsNoSlotsForVacationDay()
    {
        var specialistId = Guid.NewGuid();
        var date = new DateOnly(2026, 7, 1);
        var repository = new FakeSlotRepository
        {
            SpecialistIds = { specialistId },
            VacationDates = { (specialistId, date) }
        };
        var service = new SlotService(repository);

        var result = await service.GetAvailableSlotsAsync(specialistId, date, 30, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_RemovesSlotsConflictingWithConfirmedBookings()
    {
        var specialistId = Guid.NewGuid();
        var date = new DateOnly(2026, 7, 1);
        var repository = new FakeSlotRepository { SpecialistIds = { specialistId } };
        repository.Bookings.Add(new SlotBookingSnapshot(date, new TimeOnly(10, 0), 60));
        var service = new SlotService(repository);

        var result = await service.GetAvailableSlotsAsync(specialistId, date, 30, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(result.Value!, slot => slot.Time == new TimeOnly(10, 0));
        Assert.DoesNotContain(result.Value!, slot => slot.Time == new TimeOnly(10, 30));
        Assert.Contains(result.Value!, slot => slot.Time == new TimeOnly(11, 0));
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ValidatesInputAndMissingSpecialist()
    {
        var service = new SlotService(new FakeSlotRepository());

        var invalidSpecialist = await service.GetAvailableSlotsAsync(
            Guid.Empty,
            new DateOnly(2026, 7, 1),
            30,
            CancellationToken.None);
        var invalidDuration = await service.GetAvailableSlotsAsync(
            Guid.NewGuid(),
            new DateOnly(2026, 7, 1),
            0,
            CancellationToken.None);
        var missingSpecialist = await service.GetAvailableSlotsAsync(
            Guid.NewGuid(),
            new DateOnly(2026, 7, 1),
            30,
            CancellationToken.None);

        Assert.Equal(ResultStatus.Validation, invalidSpecialist.Status);
        Assert.Equal(ResultStatus.Validation, invalidDuration.Status);
        Assert.Equal(ResultStatus.NotFound, missingSpecialist.Status);
    }

    private sealed class FakeSlotRepository : ISlotRepository
    {
        public HashSet<Guid> SpecialistIds { get; } = [];
        public HashSet<(Guid SpecialistId, DateOnly Date)> VacationDates { get; } = [];
        public List<SlotBookingSnapshot> Bookings { get; } = [];

        public Task<bool> SpecialistExistsAsync(Guid specialistId, CancellationToken cancellationToken)
        {
            return Task.FromResult(SpecialistIds.Contains(specialistId));
        }

        public Task<bool> IsVacationDateAsync(Guid specialistId, DateOnly date, CancellationToken cancellationToken)
        {
            return Task.FromResult(VacationDates.Contains((specialistId, date)));
        }

        public Task<IReadOnlyCollection<SlotBookingSnapshot>> GetConfirmedBookingsAsync(
            Guid specialistId,
            DateOnly date,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<SlotBookingSnapshot>>(
                Bookings.Where(booking => booking.Date == date).ToArray());
        }
    }
}
