using LibraryManagement.Models;

namespace LibraryManagement.Services.Notifications;

/// <summary>
/// Decides what to say to a member and gets it sent. Callers describe the event
/// that happened; this service owns the wording, the channel choice and the log.
/// </summary>
/// <remarks>
/// The loan and reservation services depend on this interface, so neither of
/// them knows whether a message goes by email, by SMS or nowhere at all.
/// </remarks>
public interface INotificationService
{
    /// <summary>
    /// Records and attempts one message about <paramref name="kind"/> for a
    /// member. Never throws: a failed send is logged against the notification so
    /// the transaction that triggered it still completes.
    /// </summary>
    Task<Notification?> NotifyAsync(NotificationKind kind, int borrowerId, int? itemId = null,
        IReadOnlyDictionary<string, string>? facts = null);

    /// <summary>
    /// Walks the open loans and writes a due-soon or overdue message for each one
    /// that has crossed a threshold since the last run. Driven by a button on the
    /// dashboard, which stands in for the nightly job a deployed system would run.
    /// </summary>
    Task<NotificationSweepResult> RunDueDateSweepAsync();

    Task<IReadOnlyList<Notification>> GetRecentAsync(int count = 50, NotificationKind? kind = null);
    Task<NotificationSummary> GetSummaryAsync();
}

/// <summary>What one run of the due-date sweep produced.</summary>
public class NotificationSweepResult
{
    public int LoansExamined { get; set; }
    public int DueSoonSent { get; set; }
    public int OverdueSent { get; set; }
    public int Skipped { get; set; }

    public int TotalSent => DueSoonSent + OverdueSent;

    public string Describe() => TotalSent == 0
        ? $"Checked {LoansExamined} open loan{(LoansExamined == 1 ? "" : "s")}. Nothing needed a reminder today."
        : $"Checked {LoansExamined} open loan{(LoansExamined == 1 ? "" : "s")} and sent {TotalSent} " +
          $"message{(TotalSent == 1 ? "" : "s")}: {DueSoonSent} due soon, {OverdueSent} overdue.";
}

/// <summary>Headline counts for the notification dashboard.</summary>
public class NotificationSummary
{
    public int Total { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
    public int SentToday { get; set; }
    public IReadOnlyList<ChannelCount> ByChannel { get; set; } = Array.Empty<ChannelCount>();
    public IReadOnlyList<KindCount> ByKind { get; set; } = Array.Empty<KindCount>();
}

public record ChannelCount(NotificationChannel Channel, int Count);
public record KindCount(NotificationKind Kind, int Count);
