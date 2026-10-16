namespace LibraryManagement.Services;

/// <summary>
/// The answer a service gives to a command: did it work, and what should the user
/// be told. Controllers never have to guess at an exception type to build a
/// message, and the services never have to know about ModelState or TempData.
/// </summary>
public class ServiceResult
{
    protected ServiceResult(bool succeeded, string message)
    {
        Succeeded = succeeded;
        Message = message;
    }

    public bool Succeeded { get; }
    public string Message { get; }

    public static ServiceResult Ok(string message = "") => new(true, message);
    public static ServiceResult Fail(string message) => new(false, message);
}

/// <summary>A <see cref="ServiceResult"/> that also carries what was produced.</summary>
public class ServiceResult<T>
{
    private ServiceResult(bool succeeded, string message, T? value)
    {
        Succeeded = succeeded;
        Message = message;
        Value = value;
    }

    public bool Succeeded { get; }
    public string Message { get; }
    public T? Value { get; }

    public static ServiceResult<T> Ok(T value, string message = "") => new(true, message, value);
    public static ServiceResult<T> Fail(string message) => new(false, message, default);
}
