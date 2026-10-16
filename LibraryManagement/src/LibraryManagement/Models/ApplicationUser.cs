using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

/// <summary>
/// A staff login. Extends the ASP.NET Core Identity user with a display name so
/// the layout can greet people by name rather than by email address, and with
/// the branch they work at.
/// </summary>
public class ApplicationUser : IdentityUser
{
    [StringLength(120)]
    [Display(Name = "Full name")]
    public string? FullName { get; set; }

    /// <summary>
    /// The desk this person works at. Reception screens default to this branch,
    /// so staff see their own queue rather than the whole network's. Null for
    /// head office roles such as the manager, who looks across all branches.
    /// </summary>
    [Display(Name = "Works at")]
    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }
}
