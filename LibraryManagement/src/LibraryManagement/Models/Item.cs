using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Models;

/// <summary>
/// Base class for everything the library lends out. Book, MusicRecording and Toy
/// inherit from this. Entity Framework maps the whole hierarchy into a single
/// Items table using a discriminator column (Table Per Hierarchy).
/// </summary>
public abstract class Item
{
    public int Id { get; set; }

    /// <summary>
    /// The code printed on the item's label. Reception staff type this to borrow
    /// or return, so it is unique and indexed.
    /// </summary>
    [Required(ErrorMessage = "A library code is required.")]
    [StringLength(20, MinimumLength = 3)]
    [Display(Name = "Library code")]
    public string LibraryCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "A name is required.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [DataType(DataType.MultilineText)]
    public string? Description { get; set; }

    public ItemStatus Status { get; set; } = ItemStatus.Available;

    [Display(Name = "Added on")]
    [DataType(DataType.Date)]
    public DateTime AddedOn { get; set; } = DateTime.Today;

    /// <summary>
    /// The branch that physically holds this item right now. It changes only when
    /// an inter-branch transfer is received, never by editing the item directly,
    /// so the catalogue always agrees with what is on the shelf.
    /// </summary>
    [Display(Name = "Held at")]
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    /// <summary>
    /// The branch this item belongs to on the books. An item on long term loan to
    /// another location still counts against its owning branch in inventory
    /// reports, which is how a real network tracks stock it has lent out.
    /// </summary>
    [Display(Name = "Owned by")]
    public int OwningBranchId { get; set; }
    public Branch? OwningBranch { get; set; }

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<Transfer> Transfers { get; set; } = new List<Transfer>();

    /// <summary>
    /// The name of the concrete type, so views can label and group items without
    /// switching on the runtime type themselves.
    /// </summary>
    [NotMapped]
    public abstract string ItemType { get; }

    /// <summary>
    /// One line of type specific detail (author, artist, age range, ...).
    /// Each subclass decides what is worth showing. This is the polymorphic hook
    /// the catalogue and admin list views rely on.
    /// </summary>
    [NotMapped]
    public abstract string Summary { get; }

    /// <summary>An item can only leave the building when it is on the shelf.</summary>
    [NotMapped]
    public bool CanBeBorrowed => Status == ItemStatus.Available;

    /// <summary>
    /// True while the item is sitting at a branch that does not own it, which is
    /// what the inventory health report counts as stock away from home.
    /// </summary>
    [NotMapped]
    [Display(Name = "Away from home branch")]
    public bool IsAwayFromOwningBranch => BranchId != OwningBranchId;

    /// <summary>
    /// Reservable covers the states a patron is allowed to queue behind: the item
    /// exists and is expected back. A destroyed item is gone for good, so no
    /// amount of waiting will produce it.
    /// </summary>
    [NotMapped]
    public bool CanBeReserved => Status is ItemStatus.Borrowed or ItemStatus.Damaged;
}
