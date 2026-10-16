namespace LibraryManagement.Models;

/// <summary>
/// The three staff roles from the brief. Declared as constants so that the
/// [Authorize] attributes, the seeder and the navigation menu can never drift
/// apart through a typo.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Reception = "Reception";
    public const string Manager = "Manager";

    public static readonly string[] All = { Admin, Reception, Manager };
}
