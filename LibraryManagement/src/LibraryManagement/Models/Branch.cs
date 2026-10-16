using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Models;

/// <summary>
/// A physical library location. Every item is held at exactly one branch at a
/// time, and every member belongs to a home branch, which is the one their
/// reservations default to and where transfers are sent for collection.
/// </summary>
/// <remarks>
/// Introducing this class is what turns the single site system from Assignment
/// Two into a network. Items gained a <see cref="Item.BranchId"/>, loans record
/// where the transaction happened, and the manager's reports can now be read
/// either for the whole service or one location at a time.
/// </remarks>
public class Branch
{
    public int Id { get; set; }

    /// <summary>
    /// Short code used in library codes, URLs and the kiosk configuration, for
    /// example DRW for Darwin City. Unique and always stored upper case.
    /// </summary>
    [Required(ErrorMessage = "A branch code is required.")]
    [StringLength(6, MinimumLength = 2)]
    [Display(Name = "Branch code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "A branch name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Address { get; set; }

    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [StringLength(30)]
    public string? Phone { get; set; }

    /// <summary>
    /// Free text opening hours, surfaced by the public API's operating status
    /// endpoint and on the kiosk header.
    /// </summary>
    [StringLength(200)]
    [Display(Name = "Opening hours")]
    public string? OpeningHours { get; set; }

    /// <summary>
    /// A closed branch keeps its catalogue visible but accepts no loans,
    /// reservations or incoming transfers.
    /// </summary>
    [Display(Name = "Open to the public")]
    public bool IsOpen { get; set; } = true;

    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<Borrower> Members { get; set; } = new List<Borrower>();

    [NotMapped]
    [Display(Name = "Branch")]
    public string DisplayName => $"{Name} ({Code})";
}
