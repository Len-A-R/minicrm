namespace ServiceBooking.Application.Common;

public enum ResultStatus
{
    Ok = 0,
    Validation = 1,
    Conflict = 2,
    Unauthorized = 3,
    NotFound = 4
}
