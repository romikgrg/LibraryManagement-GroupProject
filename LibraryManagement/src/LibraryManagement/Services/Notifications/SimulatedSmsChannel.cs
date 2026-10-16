using LibraryManagement.Models;

namespace LibraryManagement.Services.Notifications;

/// <summary>
/// Stands in for an SMS gateway. Like the email channel it only writes to the
/// log, but it also enforces the one constraint that genuinely differs between
/// the two transports: a text message has a length limit.
/// </summary>
public class SimulatedSmsChannel : INotificationChannel
{
    /// <summary>A single GSM-7 segment. Longer messages are truncated, as a real gateway would split or reject them.</summary>
    private const int MaxLength = 160;

    private readonly ILogger<SimulatedSmsChannel> _logger;

    public SimulatedSmsChannel(ILogger<SimulatedSmsChannel> logger)
    {
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Sms;

    public bool CanReach(Borrower borrower) => !string.IsNullOrWhiteSpace(borrower.Phone);

    public string? AddressFor(Borrower borrower) => borrower.Phone;

    public Task<DeliveryResult> SendAsync(Notification notification, Borrower borrower)
    {
        if (!CanReach(borrower))
        {
            return Task.FromResult(DeliveryResult.Failed("No mobile number on the member's record."));
        }

        // The stored body stays whole for the dashboard; only what "goes out" is cut.
        var text = notification.Body.Length <= MaxLength
            ? notification.Body
            : notification.Body[..(MaxLength - 3)] + "...";

        _logger.LogInformation(
            "[SIMULATED SMS] To: {Recipient} | {Text}",
            notification.Recipient, text);

        return Task.FromResult(DeliveryResult.Ok());
    }
}
