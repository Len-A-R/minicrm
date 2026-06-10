using Microsoft.EntityFrameworkCore;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Infrastructure.Persistence;

public sealed class ServiceBookingDbContext(DbContextOptions<ServiceBookingDbContext> options) : DbContext(options)
{
    public DbSet<Specialist> Specialists => Set<Specialist>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<SpecialistService> SpecialistServices => Set<SpecialistService>();
    public DbSet<Vacation> Vacations => Set<Vacation>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingService> BookingServices => Set<BookingService>();
    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureSpecialist(modelBuilder);
        ConfigureLocation(modelBuilder);
        ConfigureService(modelBuilder);
        ConfigureSpecialistService(modelBuilder);
        ConfigureVacation(modelBuilder);
        ConfigureBooking(modelBuilder);
        ConfigureBookingService(modelBuilder);
        ConfigureClient(modelBuilder);
    }

    private static void ConfigureSpecialist(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Specialist>(entity =>
        {
            entity.HasKey(specialist => specialist.Id);
            entity.Property(specialist => specialist.FullName).HasMaxLength(100).IsRequired();
            entity.Property(specialist => specialist.Email).HasMaxLength(254).IsRequired();
            entity.Property(specialist => specialist.Phone).HasMaxLength(32).IsRequired();
            entity.Property(specialist => specialist.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(specialist => specialist.RefreshTokenHash).HasMaxLength(500);
            entity.Property(specialist => specialist.AvatarUrl).HasMaxLength(500);
            entity.Property(specialist => specialist.VenueName).HasMaxLength(160);
            entity.Property(specialist => specialist.CreatedAt).IsRequired();
            entity.HasIndex(specialist => specialist.Email).IsUnique();

            entity
                .HasOne(specialist => specialist.Location)
                .WithMany(location => (IEnumerable<Specialist>)location.Specialists)
                .HasForeignKey(specialist => specialist.LocationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Navigation(specialist => specialist.Services).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(specialist => specialist.Vacations).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(specialist => specialist.Bookings).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    private static void ConfigureLocation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(location => location.Id);
            entity.Property(location => location.Name).HasMaxLength(120).IsRequired();
            entity.Property(location => location.Address).HasMaxLength(250).IsRequired();
            entity.Property(location => location.Description).HasMaxLength(500);
            entity.Navigation(location => location.Specialists).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    private static void ConfigureService(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(service => service.Id);
            entity.Property(service => service.Name).HasMaxLength(120).IsRequired();
            entity.Property(service => service.Description).HasMaxLength(500);
            entity.Navigation(service => service.SpecialistServices).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    private static void ConfigureSpecialistService(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SpecialistService>(entity =>
        {
            entity.HasKey(service => service.Id);
            entity.Property(service => service.Price).HasPrecision(18, 2).IsRequired();
            entity.Property(service => service.DurationMinutes).IsRequired();
            entity.HasIndex(service => new { service.SpecialistId, service.ServiceId }).IsUnique();

            entity
                .HasOne(service => service.Specialist)
                .WithMany(specialist => (IEnumerable<SpecialistService>)specialist.Services)
                .HasForeignKey(service => service.SpecialistId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(service => service.Service)
                .WithMany(globalService => (IEnumerable<SpecialistService>)globalService.SpecialistServices)
                .HasForeignKey(service => service.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureVacation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vacation>(entity =>
        {
            entity.HasKey(vacation => vacation.Id);
            entity.Property(vacation => vacation.Date).IsRequired();
            entity.Property(vacation => vacation.Reason).HasMaxLength(250);
            entity.HasIndex(vacation => new { vacation.SpecialistId, vacation.Date }).IsUnique();

            entity
                .HasOne(vacation => vacation.Specialist)
                .WithMany(specialist => (IEnumerable<Vacation>)specialist.Vacations)
                .HasForeignKey(vacation => vacation.SpecialistId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureBooking(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(booking => booking.Id);
            entity.Property(booking => booking.ClientName).HasMaxLength(100).IsRequired();
            entity.Property(booking => booking.ClientPhone).HasMaxLength(32).IsRequired();
            entity.Property(booking => booking.RequestedDate).IsRequired();
            entity.Property(booking => booking.RequestedTime).IsRequired();
            entity.Property(booking => booking.Message).HasMaxLength(500);
            entity.Property(booking => booking.TotalPrice).HasPrecision(18, 2).IsRequired();
            entity.Property(booking => booking.TotalDuration).IsRequired();
            entity.Property(booking => booking.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(booking => booking.CreatedAt).IsRequired();
            entity.Property(booking => booking.ActualRevenue).HasPrecision(18, 2);
            entity.Property(booking => booking.RejectionReason).HasMaxLength(500);
            entity.Property(booking => booking.SpecialistReply).HasMaxLength(1000);
            entity.HasIndex(booking => new { booking.SpecialistId, booking.Status });
            entity.HasIndex(booking => new { booking.RequestedDate, booking.RequestedTime });

            entity
                .HasOne(booking => booking.Specialist)
                .WithMany(specialist => (IEnumerable<Booking>)specialist.Bookings)
                .HasForeignKey(booking => booking.SpecialistId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(booking => booking.Client)
                .WithMany(client => (IEnumerable<Booking>)client.Bookings)
                .HasForeignKey(booking => booking.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Navigation(booking => booking.Services).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    private static void ConfigureBookingService(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookingService>(entity =>
        {
            entity.HasKey(service => service.Id);
            entity.Property(service => service.ServiceName).HasMaxLength(120).IsRequired();
            entity.Property(service => service.Price).HasPrecision(18, 2).IsRequired();
            entity.Property(service => service.DurationMinutes).IsRequired();

            entity
                .HasOne(service => service.Booking)
                .WithMany(booking => (IEnumerable<BookingService>)booking.Services)
                .HasForeignKey(service => service.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureClient(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(client => client.Id);
            entity.Property(client => client.FullName).HasMaxLength(100).IsRequired();
            entity.Property(client => client.Phone).HasMaxLength(32).IsRequired();
            entity.Property(client => client.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(client => client.Tag).HasMaxLength(200);
            entity.Property(client => client.CreatedAt).IsRequired();
            entity.HasIndex(client => client.Phone).IsUnique();
            entity.Navigation(client => client.Bookings).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}
