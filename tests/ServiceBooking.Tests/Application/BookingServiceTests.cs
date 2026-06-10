using ServiceBooking.Application.Bookings;
using ServiceBooking.Application.Common;
using ServiceBooking.Domain.Entities;

namespace ServiceBooking.Tests.Application;

public sealed class BookingServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesBookingWithCalculatedTotalsAndClient()
    {
        var specialistId = Guid.NewGuid();
        var haircutId = Guid.NewGuid();
        var stylingId = Guid.NewGuid();
        var bookingRepository = new FakeBookingRepository
        {
            SpecialistIds = { specialistId },
            Options =
            {
                new SpecialistServiceBookingOption(haircutId, "Haircut", 40m, 45),
                new SpecialistServiceBookingOption(stylingId, "Styling", 30m, 30)
            }
        };
        var clientRepository = new FakeClientAutoCreationRepository();
        var service = CreateService(bookingRepository, clientRepository);

        var result = await service.CreateAsync(new CreateBookingRequest(
            "Alice Brown",
            "+15550909090",
            specialistId,
            [haircutId, stylingId],
            new DateOnly(2026, 7, 10),
            new TimeOnly(11, 30),
            "First visit"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(70m, result.Value?.TotalPrice);
        Assert.Equal(75, result.Value?.TotalDuration);
        Assert.Equal("Alice Brown", result.Value?.ClientName);
        Assert.Single(bookingRepository.Bookings);
        Assert.Single(clientRepository.Clients);
    }

    [Fact]
    public async Task CreateAsync_AllowsMessageOnlyRequest()
    {
        var specialistId = Guid.NewGuid();
        var bookingRepository = new FakeBookingRepository { SpecialistIds = { specialistId } };
        var service = CreateService(bookingRepository, new FakeClientAutoCreationRepository());

        var result = await service.CreateAsync(new CreateBookingRequest(
            "Alice Brown",
            "+15550909090",
            specialistId,
            [],
            new DateOnly(2026, 7, 10),
            new TimeOnly(11, 30),
            "Need consultation"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value?.TotalPrice);
        Assert.Empty(result.Value!.Services);
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidInput()
    {
        var service = CreateService(new FakeBookingRepository(), new FakeClientAutoCreationRepository());

        var invalidName = await service.CreateAsync(new CreateBookingRequest(
            "A1",
            "+15550909090",
            Guid.NewGuid(),
            [Guid.NewGuid()],
            new DateOnly(2026, 7, 10),
            new TimeOnly(11, 30),
            null), CancellationToken.None);
        var emptyBooking = await service.CreateAsync(new CreateBookingRequest(
            "Alice Brown",
            "+15550909090",
            Guid.NewGuid(),
            [],
            new DateOnly(2026, 7, 10),
            new TimeOnly(11, 30),
            null), CancellationToken.None);
        var pastDate = await service.CreateAsync(new CreateBookingRequest(
            "Alice Brown",
            "+15550909090",
            Guid.NewGuid(),
            [Guid.NewGuid()],
            new DateOnly(2026, 6, 1),
            new TimeOnly(11, 30),
            null), CancellationToken.None);

        Assert.Equal(ResultStatus.Validation, invalidName.Status);
        Assert.Equal("invalid_client_name", invalidName.Error?.Code);
        Assert.Equal("empty_booking", emptyBooking.Error?.Code);
        Assert.Equal("invalid_requested_date", pastDate.Error?.Code);
    }

    [Fact]
    public async Task CreateAsync_ReturnsNotFoundForMissingSpecialist()
    {
        var service = CreateService(new FakeBookingRepository(), new FakeClientAutoCreationRepository());

        var result = await service.CreateAsync(new CreateBookingRequest(
            "Alice Brown",
            "+15550909090",
            Guid.NewGuid(),
            [Guid.NewGuid()],
            new DateOnly(2026, 7, 10),
            new TimeOnly(11, 30),
            null), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationForServicesNotProvidedBySpecialist()
    {
        var specialistId = Guid.NewGuid();
        var bookingRepository = new FakeBookingRepository { SpecialistIds = { specialistId } };
        var service = CreateService(bookingRepository, new FakeClientAutoCreationRepository());

        var result = await service.CreateAsync(new CreateBookingRequest(
            "Alice Brown",
            "+15550909090",
            specialistId,
            [Guid.NewGuid()],
            new DateOnly(2026, 7, 10),
            new TimeOnly(11, 30),
            null), CancellationToken.None);

        Assert.Equal(ResultStatus.Validation, result.Status);
        Assert.Equal("invalid_services", result.Error?.Code);
    }

    [Fact]
    public async Task ClientAutoCreationService_ReusesExistingClientByPhone()
    {
        var repository = new FakeClientAutoCreationRepository();
        var existing = new Client("Old Name", "+15550909090");
        repository.Clients.Add(existing);
        var service = new ClientAutoCreationService(repository);

        var client = await service.GetOrCreateAsync("Alice Brown", " +15550909090 ", CancellationToken.None);

        Assert.Same(existing, client);
        Assert.Equal("Alice Brown", client.FullName);
        Assert.Single(repository.Clients);
    }

    private static ServiceBooking.Application.Bookings.BookingService CreateService(
        FakeBookingRepository bookingRepository,
        FakeClientAutoCreationRepository clientRepository)
    {
        return new ServiceBooking.Application.Bookings.BookingService(
            bookingRepository,
            new ClientAutoCreationService(clientRepository),
            new FakeDateTimeProvider(new DateTimeOffset(2026, 6, 11, 0, 0, 0, TimeSpan.Zero)));
    }

    private sealed class FakeBookingRepository : IBookingRepository
    {
        public HashSet<Guid> SpecialistIds { get; } = [];
        public List<SpecialistServiceBookingOption> Options { get; } = [];
        public List<Booking> Bookings { get; } = [];

        public Task<bool> SpecialistExistsAsync(Guid specialistId, CancellationToken cancellationToken)
        {
            return Task.FromResult(SpecialistIds.Contains(specialistId));
        }

        public Task<IReadOnlyCollection<SpecialistServiceBookingOption>> GetSpecialistServicesAsync(
            Guid specialistId,
            IReadOnlyCollection<Guid> serviceIds,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<SpecialistServiceBookingOption>>(
                Options.Where(option => serviceIds.Contains(option.ServiceId)).ToArray());
        }

        public Task AddAsync(Booking booking, CancellationToken cancellationToken)
        {
            Bookings.Add(booking);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeClientAutoCreationRepository : IClientAutoCreationRepository
    {
        public List<Client> Clients { get; } = [];

        public Task<Client?> GetByPhoneAsync(string phone, CancellationToken cancellationToken)
        {
            return Task.FromResult(Clients.SingleOrDefault(client => client.Phone == phone));
        }

        public Task AddAsync(Client client, CancellationToken cancellationToken)
        {
            Clients.Add(client);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDateTimeProvider(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
