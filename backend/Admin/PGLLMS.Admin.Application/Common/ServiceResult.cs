namespace PGLLMS.Admin.Application.Common;

public class ServiceResult
{
    public bool Succeeded { get; private set; }
    public string? ErrorMessage { get; private set; }

    private ServiceResult(bool succeeded, string? error = null)
    {
        Succeeded = succeeded;
        ErrorMessage = error;
    }

    public static ServiceResult Success() => new(true);
    public static ServiceResult Failure(string error) => new(false, error);
}

public class ServiceResult<T>
{
    public bool Succeeded { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }

    private ServiceResult(bool succeeded, T? data, string? error = null)
    {
        Succeeded = succeeded;
        Data = data;
        ErrorMessage = error;
    }

    public static ServiceResult<T> Success(T data) => new(true, data);
    public static ServiceResult<T> Failure(string error) => new(false, default, error);
}
