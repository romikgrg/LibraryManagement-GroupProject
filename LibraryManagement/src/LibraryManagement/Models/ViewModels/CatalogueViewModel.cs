namespace LibraryManagement.Models.ViewModels;

/// <summary>What the public search page needs: the query, the results and the
/// searches this visitor has already tried (kept in session).</summary>
public class CatalogueViewModel
{
    public string? Query { get; set; }
    public string? Type { get; set; }
    public bool AvailableOnly { get; set; }
    public IReadOnlyList<Item> Results { get; set; } = Array.Empty<Item>();
    public IReadOnlyList<string> RecentSearches { get; set; } = Array.Empty<string>();
    public bool HasSearched => !string.IsNullOrWhiteSpace(Query) || !string.IsNullOrWhiteSpace(Type) || AvailableOnly;
}
