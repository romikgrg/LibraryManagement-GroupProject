using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models.ViewModels;

/// <summary>The reception desk borrow form: an item code and a member.</summary>
public class BorrowViewModel
{
    [Required(ErrorMessage = "Enter the library code from the item.")]
    [Display(Name = "Library code")]
    public string LibraryCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose the borrower.")]
    [Display(Name = "Borrower")]
    public int? BorrowerId { get; set; }

    /// <summary>Populated by the controller so the view can render a picker.</summary>
    public IReadOnlyList<Borrower> Borrowers { get; set; } = Array.Empty<Borrower>();

    /// <summary>Shown after a successful loan so staff can tell the member the due date.</summary>
    public Loan? LastLoan { get; set; }
}
