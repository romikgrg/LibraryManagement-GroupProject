using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Models;

/// <summary>
/// A member of the public who is allowed to borrow items. Managed by reception.
/// </summary>
public class Borrower
{
    public int Id { get; set; }

    [Required(ErrorMessage = "A membership number is required.")]
    [StringLength(20, MinimumLength = 3)]
    [Display(Name = "Membership number")]
    public string MembershipNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "A first name is required.")]
    [StringLength(80)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "A last name is required.")]
    [StringLength(80)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150)]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(250)]
    [DataType(DataType.MultilineText)]
    public string? Address { get; set; }

    [Display(Name = "Joined on")]
    [DataType(DataType.Date)]
    public DateTime JoinedOn { get; set; } = DateTime.Today;

    /// <summary>
    /// The branch this member uses. Reservations are collected here, and it is
    /// where an inter-branch transfer is sent when they place a hold on something
    /// another location holds.
    /// </summary>
    [Display(Name = "Home branch")]
    public int HomeBranchId { get; set; }
    public Branch? HomeBranch { get; set; }

    /// <summary>
    /// How this member prefers to be contacted about due dates and holds. Drives
    /// which channel the notification service uses.
    /// </summary>
    [Display(Name = "Contact by")]
    public NotificationChannel PreferredChannel { get; set; } = NotificationChannel.Email;

    /// <summary>Suspended members keep their history but cannot borrow.</summary>
    [Display(Name = "Active member")]
    public bool IsActive { get; set; } = true;

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [NotMapped]
    [Display(Name = "Name")]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
