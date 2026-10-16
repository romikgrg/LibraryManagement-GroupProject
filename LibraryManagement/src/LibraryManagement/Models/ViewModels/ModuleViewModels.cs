using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models.ViewModels;

// ---------------------------------------------------------------------------
// Transfers
// ---------------------------------------------------------------------------

/// <summary>Everything the transfers desk shows in one screen.</summary>
public class TransferDeskViewModel
{
    public Branch? CurrentBranch { get; set; }
    public IReadOnlyList<Transfer> Incoming { get; set; } = Array.Empty<Transfer>();
    public IReadOnlyList<Transfer> Outgoing { get; set; } = Array.Empty<Transfer>();
    public IReadOnlyList<Transfer> Recent { get; set; } = Array.Empty<Transfer>();

    public bool HasAnythingOpen => Incoming.Count > 0 || Outgoing.Count > 0;
}

/// <summary>The form for asking another branch to send an item.</summary>
public class TransferRequestViewModel
{
    [Required(ErrorMessage = "Enter the library code of the item you want.")]
    [Display(Name = "Library code")]
    public string LibraryCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose the branch the item should go to.")]
    [Display(Name = "Send it to")]
    public int? ToBranchId { get; set; }

    [StringLength(300)]
    [Display(Name = "Reason")]
    public string? Reason { get; set; }

    public IReadOnlyList<Branch> Branches { get; set; } = Array.Empty<Branch>();
}

// ---------------------------------------------------------------------------
// Reservations
// ---------------------------------------------------------------------------

/// <summary>The holds desk.</summary>
public class ReservationDeskViewModel
{
    public Branch? CurrentBranch { get; set; }
    public bool ShowingAllBranches { get; set; }
    public IReadOnlyList<Reservation> Active { get; set; } = Array.Empty<Reservation>();
    public IReadOnlyList<Reservation> ReadyForCollection { get; set; } = Array.Empty<Reservation>();
    public IReadOnlyList<Reservation> Lapsed { get; set; } = Array.Empty<Reservation>();

    public IReadOnlyList<Reservation> Waiting =>
        Active.Where(r => r.Status == ReservationStatus.Waiting).ToList();
}

/// <summary>The form for putting somebody in the queue for an item.</summary>
public class PlaceReservationViewModel
{
    [Required(ErrorMessage = "Enter the library code of the item.")]
    [Display(Name = "Library code")]
    public string LibraryCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose the member placing the hold.")]
    [Display(Name = "Member")]
    public int? BorrowerId { get; set; }

    [Display(Name = "Collect from")]
    public int? CollectionBranchId { get; set; }

    public IReadOnlyList<Borrower> Borrowers { get; set; } = Array.Empty<Borrower>();
    public IReadOnlyList<Branch> Branches { get; set; } = Array.Empty<Branch>();
}

// ---------------------------------------------------------------------------
// Kiosk
// ---------------------------------------------------------------------------

/// <summary>
/// The kiosk's single screen state. One view model for the whole terminal keeps
/// the flow explicit: which step we are on, what was scanned, what to say next.
/// </summary>
public class KioskViewModel
{
    public Branch? Branch { get; set; }

    /// <summary>The code the patron scanned or typed.</summary>
    [Display(Name = "Library code")]
    public string? LibraryCode { get; set; }

    /// <summary>The membership number for a checkout or account lookup.</summary>
    [Display(Name = "Membership number")]
    public string? MembershipNumber { get; set; }

    /// <summary>The item found by the last scan, if any.</summary>
    public Item? ScannedItem { get; set; }

    /// <summary>The member found by the last lookup, if any.</summary>
    public Borrower? Member { get; set; }

    public IReadOnlyList<Loan> MemberLoans { get; set; } = Array.Empty<Loan>();
    public IReadOnlyList<Reservation> MemberHolds { get; set; } = Array.Empty<Reservation>();

    public decimal OutstandingFines { get; set; }

    /// <summary>A plain confirmation shown in large type after a successful action.</summary>
    public string? Success { get; set; }

    /// <summary>A plain, non-technical explanation when something did not work.</summary>
    public string? Problem { get; set; }

    public bool HasMember => Member is not null;
    public int OverdueCount { get; set; }
}
