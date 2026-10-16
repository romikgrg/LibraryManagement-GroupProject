using System.Text;
using System.Text.Json;
using LibraryManagement.Models;
using LibraryManagement.Models.ViewModels;
using LibraryManagement.Services;
using LibraryManagement.Services.Import;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

/// <summary>
/// Bulk loading of new stock: a CSV upload, or a search against the external
/// metadata provider.
/// </summary>
/// <remarks>
/// The CSV path is deliberately two steps. The upload is read and judged without
/// writing anything, staff see every row with its verdict, and only then do they
/// commit. The preview is carried between the two requests in TempData rather
/// than re-read from a file the server would otherwise have to keep.
/// </remarks>
[Authorize(Roles = Roles.Admin)]
public class ImportController : Controller
{
    private const string PreviewKey = "import.preview";
    private const long MaxUploadBytes = 2 * 1024 * 1024;

    private readonly IImportService _import;
    private readonly IMetadataProvider _provider;
    private readonly IBranchService _branches;
    private readonly IBranchContext _branchContext;

    public ImportController(
        IImportService import,
        IMetadataProvider provider,
        IBranchService branches,
        IBranchContext branchContext)
    {
        _import = import;
        _provider = provider;
        _branches = branches;
        _branchContext = branchContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await PopulateBranchesAsync();
        ViewData["ProviderName"] = _provider.Name;
        return View();
    }

    /// <summary>Hands staff a correctly shaped file to fill in.</summary>
    [HttpGet]
    public IActionResult Template()
    {
        var bytes = Encoding.UTF8.GetBytes(_import.BuildCsvTemplate());
        return File(bytes, "text/csv", "library-import-template.csv");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Upload(IFormFile? file, int branchId)
    {
        await PopulateBranchesAsync();
        ViewData["ProviderName"] = _provider.Name;

        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Choose a CSV file to upload.";
            return RedirectToAction(nameof(Index));
        }

        if (file.Length > MaxUploadBytes)
        {
            TempData["Error"] = "That file is larger than 2 MB. Split it into smaller batches.";
            return RedirectToAction(nameof(Index));
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "The importer reads .csv files. Save your spreadsheet as CSV and try again.";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = file.OpenReadStream();
        var preview = await _import.PreviewCsvAsync(stream, branchId);
        preview.FileName = file.FileName;

        if (preview.FileError is not null)
        {
            TempData["Error"] = preview.FileError;
            return RedirectToAction(nameof(Index));
        }

        // Carried to the confirm step so the file does not have to be stored.
        TempData[PreviewKey] = JsonSerializer.Serialize(preview);

        return View("Preview", preview);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm()
    {
        if (TempData[PreviewKey] is not string json)
        {
            TempData["Error"] = "That preview has expired. Please upload the file again.";
            return RedirectToAction(nameof(Index));
        }

        var preview = JsonSerializer.Deserialize<ImportPreview>(json);
        if (preview is null)
        {
            TempData["Error"] = "That preview could not be read. Please upload the file again.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _import.CommitAsync(preview, preview.BranchId);

        if (result.Succeeded)
        {
            TempData["Success"] = result.Describe();
        }
        else
        {
            TempData["Error"] = result.Describe();
        }

        return RedirectToAction("Index", "Items");
    }

    /// <summary>Searches the external provider and shows what could be imported.</summary>
    [HttpGet]
    public async Task<IActionResult> Provider(string? query, string? type)
    {
        await PopulateBranchesAsync();

        ViewData["ProviderName"] = _provider.Name;
        ViewData["Query"] = query;
        ViewData["Type"] = type;

        var results = string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(type)
            ? await _provider.SearchAsync(string.Empty, null)
            : await _provider.SearchAsync(query ?? string.Empty, type);

        return View(results);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportSelected(string[] identifiers, int branchId)
    {
        if (identifiers.Length == 0)
        {
            TempData["Error"] = "Tick at least one record to import.";
            return RedirectToAction(nameof(Provider));
        }

        var result = await _import.ImportFromProviderAsync(identifiers, branchId);

        if (result.Succeeded)
        {
            TempData["Success"] = result.Describe();
        }
        else
        {
            TempData["Error"] = result.Describe();
        }

        return RedirectToAction("Index", "Items");
    }

    /// <summary>
    /// Fills the branch picker and preselects the one this instance is running
    /// as, so an administrator at a branch does not have to choose it every time.
    /// </summary>
    private async Task PopulateBranchesAsync()
    {
        ViewData["Branches"] = await _branches.GetOpenAsync();
        ViewData["DefaultBranchId"] = await _branchContext.GetOperatingBranchIdAsync();
    }
}
