namespace LibraryManagement.Services.Notifications;

/// <summary>
/// Notification policy, bound from the "Notifications" section of
/// appsettings.json. Turning reminders off, or changing how early they go out,
/// is a configuration change rather than a code change.
/// </summary>
public class NotificationRules
{
    public const string SectionName = "Notifications";

    /// <summary>Master switch. Off means nothing is composed, sent or logged.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How many days before the due date a reminder goes out.</summary>
    public int DueSoonDays { get; set; } = 3;

    /// <summary>How long a member has to collect a hold before it passes on.</summary>
    public int HoldShelfDays { get; set; } = 7;
}
