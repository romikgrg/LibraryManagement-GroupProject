namespace LibraryManagement.Services.Reporting;

/// <summary>Which report the manager is exporting.</summary>
public enum ReportKind
{
    /// <summary>Loans, by month, type and branch.</summary>
    Borrowing = 0,

    /// <summary>Fines charged, collected and outstanding.</summary>
    FineRevenue = 1,

    /// <summary>Stock position per branch, with availability and items out of action.</summary>
    InventoryHealth = 2
}

/// <summary>A finished export, ready to be handed to the browser.</summary>
public record ExportedFile(string FileName, string ContentType, byte[] Content);

/// <summary>
/// Turns the manager's reports into files that leave the application.
/// </summary>
/// <remarks>
/// The two formats are deliberately behind one interface. A controller asks for
/// a report and a format; it never knows whether a CSV writer or a PDF renderer
/// produced the bytes, so adding a third format is a new branch here rather than
/// a new action on the controller.
/// </remarks>
public interface IReportExportService
{
    Task<ExportedFile> ExportCsvAsync(ReportKind kind, int? branchId = null);
    Task<ExportedFile> ExportPdfAsync(ReportKind kind, int? branchId = null);
}
