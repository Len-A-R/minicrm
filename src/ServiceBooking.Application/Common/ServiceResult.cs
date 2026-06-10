namespace ServiceBooking.Application.Common;

public sealed class ServiceResult<T>
{
    private ServiceResult(T value)
    {
        IsSuccess = true;
        Value = value;
        Status = ResultStatus.Ok;
    }

    private ServiceResult(ResultStatus status, ServiceError error)
    {
        IsSuccess = false;
        Status = status;
        Error = error;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public ResultStatus Status { get; }
    public ServiceError? Error { get; }

    public static ServiceResult<T> Success(T value) => new(value);

    public static ServiceResult<T> Failure(ResultStatus status, string code, string message)
    {
        return new ServiceResult<T>(status, new ServiceError(code, message));
    }
}
