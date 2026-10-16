namespace LibraryManagement.Models.ViewModels;

/// <summary>Filters for the admin item list.</summary>
public class ItemSearchViewModel
{
    public string? Query { get; set; }
    public string? Type { get; set; }
    public ItemStatus? Status { get; set; }

    /// <summary>Restrict to one branch's shelves, or null for the whole network.</summary>
    public int? BranchId { get; set; }

    public IReadOnlyList<Branch> Branches { get; set; } = Array.Empty<Branch>();
    public IReadOnlyList<Item> Results { get; set; } = Array.Empty<Item>();
}
