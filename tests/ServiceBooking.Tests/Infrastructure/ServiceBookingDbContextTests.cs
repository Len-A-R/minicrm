using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;
using ServiceBooking.Infrastructure.Persistence;

namespace ServiceBooking.Tests.Infrastructure;

public sealed class ServiceBookingDbContextTests
{
    [Fact]
    public async Task Database_CanCreateSchemaAndPersistBookingGraph()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ServiceBookingDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var dbContext = new ServiceBookingDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();

            var location = new Location("Central Clinic", "100 Market Street", "Main office");
            var specialist = new Specialist("Dr Jane Doe", "jane@example.com", "+15550101010", "hashed-password");
            specialist.UpdateProfile("Dr Jane Doe", "+15550101010", "Central Clinic", location.Id);

            var client = new Client("Alice Brown", "+15550909090");
            var service = new Service("Consultation", "Initial appointment");
            var specialistService = new SpecialistService(specialist.Id, service.Id, 150m, 60);
            var vacation = new Vacation(specialist.Id, new DateOnly(2026, 8, 1), "Vacation");
            var booking = new Booking(
                client.FullName,
                client.Phone,
                specialist.Id,
                new DateOnly(2026, 8, 2),
                new TimeOnly(11, 0),
                [new BookingService(service.Id, service.Name, 150m, 60)],
                "First visit",
                client.Id);

            dbContext.AddRange(location, specialist, client, service, specialistService, vacation, booking);
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = new ServiceBookingDbContext(options))
        {
            var booking = await dbContext.Bookings
                .Include(item => item.Services)
                .Include(item => item.Client)
                .SingleAsync();

            Assert.Equal(BookingStatus.New, booking.Status);
            Assert.Equal(150m, booking.TotalPrice);
            Assert.Equal(60, booking.TotalDuration);
            Assert.Single(booking.Services);
            Assert.Equal("Alice Brown", booking.Client?.FullName);
        }
    }

    [Fact]
    public async Task Database_EnforcesUniqueIndexes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ServiceBookingDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new ServiceBookingDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        dbContext.Clients.Add(new Client("First Client", "+15550000000"));
        dbContext.Clients.Add(new Client("Second Client", "+15550000000"));

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }
}
