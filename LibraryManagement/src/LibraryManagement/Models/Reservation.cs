using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Models;

/// <summary>Where a hold sits in its life.</summary>
public enum ReservationStatus
{
    /// <summary>In the queue, waiting for the item to come back.</summary>
    [Display(Name = "Waiting")] Waiting = 0,

    /// <summary>The item is on the holds shelf and the member has been told.</summary>
    [Display(Name = "Ready for collection")] Ready = 1,

    /// <summary>The member came in and borrowed it. The hold is done.</summary>
    [Display(Name = "Collected")] Collected = 2,

    /// <summary>Called off by the member or by staff.</summary>
    [Display(Name = "Cancelled")] Cancelled = 3,

    /// <summary>Not collected inside the holding window, so it passed to the next in line.</summary>
    [Display(Name = "Expired")] Expired = 4
}

/// <summary>
/// One patron's place in the queue for one item.
/// </summary>
/// <remarks>
/// Position in the queue is derived from <see cref="PlacedOn"/> rather than
/// stored as a number. Storing positions would mean rewriting every row below
/// whenever somebody cancels, and any missed update would leave two people
/// holding the same place. Ordering by the time they joined cannot drift.
/// </remarks>
public class Reservation
{
    public int Id { get; set; }

    public int ItemId { get; set; }
    public Item? Item { get; set; }

    public int BorrowerId { get; set; }
    public Borrower? Borrower { get; set; }

    /// <summary>Where the member wants to collect it, defaulting to their home branch.</summary>
    [Display(Name = "Collect from")]
    public int CollectionBranchId { get; set; }
    public Branch? CollectionBranch { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Waiting;

    [Display(Name = "Placed on")]
    public DateTime PlacedOn { get; set; }

    /// <summary>When the item hit the holds shelf and the clock started running.</summary>
    [Display(Name = "Ready on")]
    public DateTime? ReadyOn { get; set; }

    /// <summary>The last day the member can collect before the hold passes on.</summary>
    [Display(Name = "Collect by")]
    public DateTime? ExpiresOn { get; set; }

    [Display(Name = "Closed on")]
    public DateTime? ClosedOn { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }

    [NotMapped]
    public bool IsActive => Status is ReservationStatus.Waiting or ReservationStatus.Ready;

    /// <summary>True once the collection window has run out and nobody has come in.</summary>
    public bool HasLapsed(DateTime asAt) =>
        Status == ReservationStatus.Ready && ExpiresOn.HasValue && ExpiresOn.Value.Date < asAt.Date;
}
