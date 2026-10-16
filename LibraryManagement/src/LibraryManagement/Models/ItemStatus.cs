using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

/// <summary>
/// The lifecycle states an item can be in. Stored as an int in the database.
/// </summary>
/// <remarks>
/// The first four are the states Assignment Two required. The last two arrived
/// with the multi-branch and reservation modules: an item on a truck between
/// branches and an item waiting on the holds shelf are both unavailable, but for
/// different reasons, and reception needs to tell them apart at a glance.
/// New values are appended so the stored integers of existing rows keep their
/// meaning.
/// </remarks>
public enum ItemStatus
{
    [Display(Name = "Available")]
    Available = 0,

    [Display(Name = "Borrowed")]
    Borrowed = 1,

    [Display(Name = "Damaged")]
    Damaged = 2,

    [Display(Name = "Destroyed")]
    Destroyed = 3,

    /// <summary>Dispatched from one branch and not yet checked in at the other.</summary>
    [Display(Name = "In transit")]
    InTransit = 4,

    /// <summary>On the holds shelf, set aside for the member whose reservation it filled.</summary>
    [Display(Name = "Awaiting collection")]
    AwaitingCollection = 5
}
