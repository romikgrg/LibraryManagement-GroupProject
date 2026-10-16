using LibraryManagement.Models;

namespace LibraryManagement.Services.Notifications;

/// <summary>
/// One way of getting a message to a member.
/// </summary>
/// <remarks>
/// This is the seam the whole notification module turns on. The service that
/// decides <em>what</em> to say depends on this interface and never on a
/// particular transport, so swapping the simulated senders for a real provider
/// is a new class and one line in Program.cs. Several implementations are
/// registered at once and chosen at run time by
/// <see cref="INotificationChannel.Channel"/>, which is the standard .NET way of
/// doing strategy selection through the container.
/// </remarks>
public interface INotificationChannel
{
    /// <summary>Which channel this implementation serves.</summary>
    NotificationChannel Channel { get; }

    /// <summary>
    /// Whether this member can be reached on this channel at all. An email
    /// channel cannot do anything for somebody with no email address, and saying
    /// so up front lets the service fall back rather than record a failure.
    /// </summary>
    bool CanReach(Borrower borrower);

    /// <summary>The address or number this channel would use for the member.</summary>
    string? AddressFor(Borrower borrower);

    /// <summary>
    /// Attempts delivery. Returns the outcome rather than throwing, because a
    /// bounced message is an ordinary event that belongs in the log, not an
    /// exception that should abandon the borrow transaction that triggered it.
    /// </summary>
    Task<DeliveryResult> SendAsync(Notification notification, Borrower borrower);
}

/// <summary>What happened when a channel tried to deliver.</summary>
public record DeliveryResult(bool Succeeded, string? FailureReason = null)
{
    public static DeliveryResult Ok() => new(true);
    public static DeliveryResult Failed(string reason) => new(false, reason);
}
