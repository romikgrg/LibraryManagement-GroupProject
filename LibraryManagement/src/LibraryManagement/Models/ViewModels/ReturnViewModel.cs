using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models.ViewModels;

/// <summary>The reception desk return form.</summary>
public class ReturnViewModel
{
    [Required(ErrorMessage = "Enter the library code from the item.")]
    [Display(Name = "Library code")]
    public string LibraryCode { get; set; } = string.Empty;

    [Display(Name = "Condition on return")]
    public ReturnCondition Condition { get; set; } = ReturnCondition.Good;

    [StringLength(500)]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    /// <summary>Filled in after a return so the view can show the fine owed.</summary>
    public Loan? CompletedLoan { get; set; }
}

/// <summary>How the item came back, which decides the status it goes to.</summary>
public enum ReturnCondition
{
    [Display(Name = "Good - back on the shelf")] Good = 0,
    [Display(Name = "Damaged - needs repair")] Damaged = 1,
    [Display(Name = "Beyond repair - destroy")] Destroyed = 2
}
