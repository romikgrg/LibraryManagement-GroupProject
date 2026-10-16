using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Models;

/// <summary>
/// One borrowing transaction: which item went out, to whom, when it was due and
/// what it cost if it came back late. Loans are never deleted, so they double as
/// the borrowing history the manager reports on.
/// </summary>
public class Loan
{
    public int Id { get; set; }

    public int ItemId { get; set; }
    public Item? Item { get; set; }

    public int BorrowerId { get; set; }
    public Borrower? Borrower { get; set; }

    /// <summary>
    /// The desk the item went out from. Recorded on the loan rather than read
    /// from the item, because the item may be transferred elsewhere before it
    /// comes back and the manager's per-branch figures must still credit the
    /// branch that did the lending.
    /// </summary>
    [Display(Name = "Borrowed at")]
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    /// <summary>The desk that took the item back, which may be a different branch.</summary>
    [Display(Name = "Returned at")]
    public int? ReturnBranchId { get; set; }
    public Branch? ReturnBranch { get; set; }

    [Display(Name = "Borrowed on")]
    [DataType(DataType.Date)]
    public DateTime BorrowedOn { get; set; }

    [Display(Name = "Due on")]
    [DataType(DataType.Date)]
    public DateTime DueOn { get; set; }

    [Display(Name = "Returned on")]
    [DataType(DataType.Date)]
    public DateTime? ReturnedOn { get; set; }

    [Display(Name = "Fine")]
    [DataType(DataType.Currency)]
    public decimal FineAmount { get; set; }

    [Display(Name = "Fine paid")]
    public bool FinePaid { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [NotMapped]
    public bool IsOpen => ReturnedOn == null;

    [NotMapped]
    [Display(Name = "Outstanding fine")]
    public decimal OutstandingFine => FinePaid ? 0m : FineAmount;

    /// <summary>How late the item is (or was), in whole days. Never negative.</summary>
    public int DaysOverdue(DateTime asAt)
    {
        var endDate = ReturnedOn ?? asAt;
        var days = (endDate.Date - DueOn.Date).Days;
        return days > 0 ? days : 0;
    }
}
