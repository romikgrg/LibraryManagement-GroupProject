namespace LibraryManagement.Services;

/// <summary>
/// Wraps the system clock so that fines and overdue reports depend on an
/// abstraction rather than on DateTime.Now. A test can supply a clock parked on
/// any date and check the fine that comes out.
/// </summary>
public interface IClock
{
    DateTime Now { get; }
    DateTime Today { get; }
}

public class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
    public DateTime Today => DateTime.Today;
}
