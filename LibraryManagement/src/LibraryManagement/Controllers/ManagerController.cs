using LibraryManagement.Models;
using LibraryManagement.Services;
using LibraryManagement.Services.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

/// <summary>
/// Reporting for the library manager. Read only by design: the manager looks at
/// numbers, the other two roles change the data behind them.
/// </summary>
/// <remarks>
/// The group project widened this from a single dashboard to a dashboard that
/// can be read for the whole network or one branch, plus exports of the three
/// reports in CSV and PDF.
/// </remarks>
[Authorize(Roles = Roles.Manager)]
public class ManagerController : Controller
{
    private readonly IStatisticsService _statistics;
    private readonly IBranchService _branches;
    private readonly IReportExportService _exports;

    public ManagerController(
        IStatisticsService statistics,
        IBranchService branches,
        IReportExportService exports)
    {
        _statistics = statistics;
        _branches = branches;
        _exports = exports;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? branchId)
    {
        ViewData["Branches"] = await _branches.GetAllAsync();
        ViewData["BranchId"] = branchId;

        if (branchId.HasValue)
        {
            var branch = await _branches.GetAsync(branchId.Value);
            ViewData["BranchName"] = branch?.DisplayName;
        }

        return View(await _statistics.BuildDashboardAsync(branchId));
    }

    /// <summary>Stock position for every branch, side by side.</summary>
    [HttpGet]
    public async Task<IActionResult> Inventory()
    {
        var branches = await _branches.GetAllAsync();

        var inventories = new List<BranchInventory>();
        foreach (var branch in branches)
        {
            inventories.Add(await _branches.GetInventoryAsync(branch.Id));
        }

        return View(inventories);
    }

    /// <summary>
    /// Streams a report as a file. The service decides the bytes, the content
    /// type and the file name, so this action is the same three lines whichever
    /// report and format are asked for.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Export(ReportKind report, string format = "csv", int? branchId = null)
    {
        var isPdf = string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase);

        var file = isPdf
            ? await _exports.ExportPdfAsync(report, branchId)
            : await _exports.ExportCsvAsync(report, branchId);

        return File(file.Content, file.ContentType, file.FileName);
    }
}
