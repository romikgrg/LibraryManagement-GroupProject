using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Models;

/// <summary>
/// The states an inter-branch transfer moves through. The order matters: a
/// transfer only ever moves forward, or sideways into <see cref="Cancelled"/>.
/// </summary>
public enum TransferStatus
{
    /// <summary>Asked for by the destination, not yet acted on by the sender.</summary>
    [Display(Name = "Requested")] Requested = 0,

    /// <summary>Taken off the shelf and sent. The item is in the van, owned by neither desk.</summary>
    [Display(Name = "In transit")] InTransit = 1,

    /// <summary>Checked in at the destination. The item's branch has changed.</summary>
    [Display(Name = "Received")] Received = 2,

    /// <summary>Called off before dispatch, or the item turned out not to be there.</summary>
    [Display(Name = "Cancelled")] Cancelled = 3
}

/// <summary>
/// One item moving from one branch to another: requested by the destination,
/// dispatched by the holder, received at the far end.
/// </summary>
/// <remarks>
/// The item's <see cref="Item.BranchId"/> is deliberately not updated at dispatch
/// time. While a transfer is in transit the item still shows at the sending
/// branch and its status is <see cref="ItemStatus.InTransit"/>, so nobody can
/// lend out a box that is on a truck. Only receiving moves it.
/// </remarks>
public class Transfer
{
    public int Id { get; set; }

    public int ItemId { get; set; }
    public Item? Item { get; set; }

    [Display(Name = "From")]
    public int FromBranchId { get; set; }
    public Branch? FromBranch { get; set; }

    [Display(Name = "To")]
    public int ToBranchId { get; set; }
    public Branch? ToBranch { get; set; }

    public TransferStatus Status { get; set; } = TransferStatus.Requested;

    [Display(Name = "Requested on")]
    public DateTime RequestedOn { get; set; }

    [Display(Name = "Dispatched on")]
    public DateTime? DispatchedOn { get; set; }

    [Display(Name = "Received on")]
    public DateTime? ReceivedOn { get; set; }

    /// <summary>
    /// Set when the transfer exists to satisfy a hold, so receiving staff know to
    /// put the item on the holds shelf rather than back into general stock.
    /// </summary>
    public int? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    [StringLength(300)]
    public string? Reason { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [NotMapped]
    public bool IsOpen => Status is TransferStatus.Requested or TransferStatus.InTransit;

    /// <summary>How long the transfer has been running, for the overdue-in-transit report.</summary>
    public int DaysInProgress(DateTime asAt)
    {
        var end = ReceivedOn ?? asAt;
        var days = (end.Date - RequestedOn.Date).Days;
        return days > 0 ? days : 0;
    }
}
