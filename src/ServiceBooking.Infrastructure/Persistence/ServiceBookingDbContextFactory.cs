using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ServiceBooking.Infrastructure.Persistence;

[ExcludeFromCodeCoverage]
public sealed class ServiceBookingDbContextFactory : IDesignTimeDbContextFactory<ServiceBookingDbContext>
{
    public ServiceBookingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ServiceBookingDbContext>()
            .UseSqlite("Data Source=service-booking.db")
            .Options;

        return new ServiceBookingDbContext(options);
    }
}
