using LibraryManagement.Models;

namespace LibraryManagement.Services.Notifications;

/// <summary>
/// Turns an event into the words a member reads.
/// </summary>
/// <remarks>
/// Kept separate from <see cref="NotificationService"/> so that the wording can
/// be reviewed, translated or unit tested on its own, without a database or a
/// channel in the way. It is static and pure: same inputs, same message, every
/// time.
/// </remarks>
public static class NotificationComposer
{
    public static (string Subject, string Body) Compose(
        NotificationKind kind,
        Borrower borrower,
        Item? item,
        IReadOnlyDictionary<string, string>? facts)
    {
        var name = borrower.FirstName;
        var title = item?.Name ?? "an item";
        var code = item is null ? "" : $" ({item.LibraryCode})";

        string Fact(string key, string fallback = "") =>
            facts is not null && facts.TryGetValue(key, out var value) ? value : fallback;

        return kind switch
        {
            NotificationKind.Borrowed => (
                "Item borrowed",
                $"Hi {name}, you borrowed {title}{code}. It is due back on {Fact("dueOn", "the date on your receipt")}."),

            NotificationKind.DueSoon => (
                "Due back soon",
                $"Hi {name}, {title}{code} is due back on {Fact("dueOn")}. " +
                $"That is {Fact("days", "a few")} day(s) away. Renew or return it to avoid a fine."),

            NotificationKind.Overdue => (
                "Item overdue",
                $"Hi {name}, {title}{code} was due on {Fact("dueOn")} and is now " +
                $"{Fact("days", "several")} day(s) overdue. Fines are accruing daily."),

            NotificationKind.FineCharged => (
                "Fine charged",
                $"Hi {name}, a fine of {Fact("amount", "an amount")} has been charged for the late return " +
                $"of {title}{code}. You can pay this at any branch."),

            NotificationKind.ReservationReady => (
                "Your hold is ready",
                $"Hi {name}, {title}{code} is ready to collect from {Fact("branch", "your branch")}. " +
                $"Please pick it up by {Fact("expiresOn", "the date shown on your account")}."),

            NotificationKind.ReservationExpired => (
                "Hold expired",
                $"Hi {name}, the hold on {title}{code} was not collected in time and has passed to the " +
                "next member in the queue. You are welcome to place it again."),

            NotificationKind.TransferArrived => (
                "Item arrived at your branch",
                $"Hi {name}, {title}{code} has arrived at {Fact("branch", "your branch")} and is ready for you."),

            _ => (
                "Library notice",
                $"Hi {name}, there is an update on your library account.")
        };
    }
}
