using LibraryManagement.Models.ViewModels;

namespace LibraryManagement.Services.Import;

/// <summary>
/// Bulk loading of new stock, from a CSV file or from the external metadata
/// provider.
/// </summary>
public interface IImportService
{
    /// <summary>
    /// Reads a CSV without writing anything, so staff can see exactly what would
    /// happen before committing. Every row comes back with its own verdict.
    /// </summary>
    Task<ImportPreview> PreviewCsvAsync(Stream csv, int branchId);

    /// <summary>Writes the rows that passed validation. Rows with errors are left out.</summary>
    Task<ImportResult> CommitAsync(ImportPreview preview, int branchId);

    /// <summary>Turns chosen provider records into catalogue items.</summary>
    Task<ImportResult> ImportFromProviderAsync(IEnumerable<string> identifiers, int branchId);

    /// <summary>The header row and one example line, offered as a download.</summary>
    string BuildCsvTemplate();
}
