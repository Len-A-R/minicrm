namespace ServiceBooking.Application.Common;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
