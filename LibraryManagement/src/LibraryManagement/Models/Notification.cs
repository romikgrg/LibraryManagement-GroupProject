using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Models;

/// <summary>How a message reaches a member.</summary>
public enum NotificationChannel
{
    [Display(Name = "Email")] Email = 0,
    [Display(Name = "SMS")] Sms = 1
}

/// <summary>Why the system wrote to somebody.</summary>
public enum NotificationKind
{
    [Display(Name = "Item borrowed")] Borrowed = 0,
    [Display(Name = "Due soon")] DueSoon = 1,
    [Display(Name = "Overdue")] Overdue = 2,
    [Display(Name = "Fine charged")] FineCharged = 3,
    [Display(Name = "Hold ready")] ReservationReady = 4,
    [Display(Name = "Hold expired")] ReservationExpired = 5,
    [Display(Name = "Transfer arrived")] TransferArrived = 6
}

/// <summary>Whether the send worked.</summary>
public enum NotificationStatus
{
    [Display(Name = "Queued")] Queued = 0,
    [Display(Name = "Sent")] Sent = 1,
    [Display(Name = "Failed")] Failed = 2,
    [Display(Name = "Skipped")] Skipped = 3
}

/// <summary>
/// A record of one message the system sent, or tried to send, to a member.
/// </summary>
/// <remarks>
/// Nothing actually leaves the building. The simulated channels write the
/// message here and to the application log, and the staff dashboard reads this
/// table. Keeping the log in the database rather than only in the console means
/// the behaviour is demonstrable to a marker and auditable by a manager, and it
/// is also exactly what a real implementation would keep alongside a live
/// provider so that failures can be retried.
/// </remarks>
public class Notification
{
    public int Id { get; set; }

    public int BorrowerId { get; set; }
    public Borrower? Borrower { get; set; }

    /// <summary>The item the message is about, when there is one.</summary>
    public int? ItemId { get; set; }
    public Item? Item { get; set; }

    public NotificationKind Kind { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Queued;

    /// <summary>The email address or mobile number the message went to.</summary>
    [Required]
    [StringLength(150)]
    public string Recipient { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    [DataType(DataType.MultilineText)]
    public string Body { get; set; } = string.Empty;

    [Display(Name = "Created")]
    public DateTime CreatedOn { get; set; }

    [Display(Name = "Sent")]
    public DateTime? SentOn { get; set; }

    /// <summary>Why a send was skipped or failed, shown on the dashboard.</summary>
    [StringLength(300)]
    [Display(Name = "Outcome")]
    public string? FailureReason { get; set; }

    [NotMapped]
    public bool Delivered => Status == NotificationStatus.Sent;
}
