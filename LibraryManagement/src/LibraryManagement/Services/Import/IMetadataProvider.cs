using LibraryManagement.Models.ViewModels;

namespace LibraryManagement.Services.Import;

/// <summary>
/// An outside source of catalogue metadata, looked up by ISBN, barcode or title.
/// </summary>
/// <remarks>
/// The importer depends on this interface, not on any particular provider, so
/// the mock used for the assignment can be replaced with a real call to Open
/// Library or Trove by writing one more implementation and changing the
/// registration in Program.cs. Nothing in the import pipeline would change.
/// </remarks>
public interface IMetadataProvider
{
    /// <summary>A name for the provider, shown to staff on the import screen.</summary>
    string Name { get; }

    /// <summary>
    /// Looks up candidate records. Returns an empty list rather than throwing
    /// when nothing matches, because "no results" is an ordinary answer.
    /// </summary>
    Task<IReadOnlyList<MetadataResult>> SearchAsync(string query, string? type = null, int maxResults = 10);

    /// <summary>Fetches one record by its identifier in the provider's own catalogue.</summary>
    Task<MetadataResult?> GetByIdentifierAsync(string identifier);
}
