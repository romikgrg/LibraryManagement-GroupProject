using LibraryManagement.Models;
using LibraryManagement.Models.ViewModels;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

/// <summary>
/// The holds desk: who is waiting for what, what is on the holds shelf, and
/// which holds have run out of time.
/// </summary>
[Authorize(Roles = Roles.Reception)]
public class ReservationsController : Controller
{
    private readonly IReservationService _reservations;
    private readonly IBorrowerService _borrowers;
    private readonly IBranchService _branches;
    private readonly IBranchContext _branchContext;
    private readonly IClock _clock;

    public ReservationsController(
        IReservationService reservations,
        IBorrowerService borrowers,
        IBranchService branches,
        IBranchContext branchContext,
        IClock clock)
    {
        _reservations = reservations;
        _borrowers = borrowers;
        _branches = branches;
        _branchContext = branchContext;
        _clock = clock;
    }

    [HttpGet]
    public async Task<IActionResult> Index(bool allBranches = false)
    {
        var branch = await _branchContext.GetCurrentBranchAsync();
        var scope = allBranches ? null : branch?.Id;

        var active = await _reservations.GetActiveAsync(scope);
        var today = _clock.Today;

        return View(new ReservationDeskViewModel
        {
            CurrentBranch = branch,
            ShowingAllBranches = allBranches || branch is null,
            Active = active,
            ReadyForCollection = active.Where(r => r.Status == ReservationStatus.Ready).ToList(),
            Lapsed = active.Where(r => r.HasLapsed(today)).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Place(string? libraryCode)
    {
        return View(new PlaceReservationViewModel
        {
            LibraryCode = libraryCode ?? string.Empty,
            Borrowers = await _borrowers.GetActiveAsync(),
            Branches = await _branches.GetOpenAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Place(PlaceReservationViewModel form)
    {
        if (!ModelState.IsValid)
        {
            form.Borrowers = await _borrowers.GetActiveAsync();
            form.Branches = await _branches.GetOpenAsync();
            return View(form);
        }

        var result = await _reservations.PlaceAsync(form.LibraryCode, form.BorrowerId!.Value, form.CollectionBranchId);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            form.Borrowers = await _borrowers.GetActiveAsync();
            form.Branches = await _branches.GetOpenAsync();
            return View(form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>The queue for one item, in the order people joined it.</summary>
    [HttpGet]
    public async Task<IActionResult> Queue(int itemId)
    {
        var queue = await _reservations.GetQueueAsync(itemId);
        ViewData["ItemName"] = queue.FirstOrDefault()?.Item?.Name ?? "this item";
        return View(queue);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason, string? returnUrl)
    {
        var result = await _reservations.CancelAsync(id, reason);

        if (result.Succeeded)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        // Only ever redirect inside this site, so a tampered query string cannot
        // send staff to somebody else's page.
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Closes holds nobody collected and offers each item to the next in line.
    /// Stands in for the nightly job a deployed system would run.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExpireLapsed()
    {
        var count = await _reservations.ExpireLapsedAsync();

        TempData["Success"] = count == 0
            ? "No holds had run out of time."
            : $"Closed {count} uncollected hold{(count == 1 ? "" : "s")} and offered each item to the next member in the queue.";

        return RedirectToAction(nameof(Index));
    }
}
