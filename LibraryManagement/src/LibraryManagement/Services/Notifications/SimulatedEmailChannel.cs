using LibraryManagement.Models;

namespace LibraryManagement.Services.Notifications;

/// <summary>
/// Stands in for an SMTP client. Nothing leaves the building: the message is
/// written to the application log in the shape a real send would take, and the
/// caller records it in the notifications table for the staff dashboard.
/// </summary>
/// <remarks>
/// Swapping this for a live sender means writing a class that implements
/// <see cref="INotificationChannel"/> around MailKit or SendGrid and changing
/// the registration in Program.cs. No caller changes, because none of them know
/// this class exists.
/// </remarks>
public class SimulatedEmailChannel : INotificationChannel
{
    private readonly ILogger<SimulatedEmailChannel> _logger;

    public SimulatedEmailChannel(ILogger<SimulatedEmailChannel> logger)
    {
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Email;

    public bool CanReach(Borrower borrower) => !string.IsNullOrWhiteSpace(borrower.Email);

    public string? AddressFor(Borrower borrower) => borrower.Email;

    public Task<DeliveryResult> SendAsync(Notification notification, Borrower borrower)
    {
        if (!CanReach(borrower))
        {
            return Task.FromResult(DeliveryResult.Failed("No email address on the member's record."));
        }

        _logger.LogInformation(
            "[SIMULATED EMAIL] To: {Recipient} | Subject: {Subject} | {Body}",
            notification.Recipient, notification.Subject, notification.Body);

        return Task.FromResult(DeliveryResult.Ok());
    }
}
