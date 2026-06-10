using ServiceBooking.Application.Common;

namespace ServiceBooking.Infrastructure;

public sealed class SystemClock : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
