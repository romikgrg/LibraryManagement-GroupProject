using LibraryManagement.Models;
using LibraryManagement.Models.ViewModels;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

/// <summary>
/// The inter-branch transfers desk. Reception staff raise requests for stock
/// held elsewhere, send items the other way, and check in what arrives.
/// </summary>
[Authorize(Roles = Roles.Reception)]
public class TransfersController : Controller
{
    private readonly ITransferService _transfers;
    private readonly IBranchService _branches;
    private readonly IBranchContext _branchContext;

    public TransfersController(ITransferService transfers, IBranchService branches, IBranchContext branchContext)
    {
        _transfers = transfers;
        _branches = branches;
        _branchContext = branchContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var branch = await _branchContext.GetCurrentBranchAsync();

        var model = new TransferDeskViewModel
        {
            CurrentBranch = branch,
            Incoming = branch is null
                ? await _transfers.GetOpenAsync()
                : await _transfers.GetIncomingAsync(branch.Id),
            Outgoing = branch is null
                ? Array.Empty<Transfer>()
                : await _transfers.GetOutgoingAsync(branch.Id),
            Recent = await _transfers.GetRecentAsync()
        };

        return View(model);
    }

    /// <summary>
    /// Raises a request for an item held at another branch.
    /// </summary>
    /// <remarks>
    /// The method is called RequestItem rather than Request because
    /// <see cref="ControllerBase.Request"/> already exists and names the incoming
    /// HttpRequest. A method with the same name compiles but hides it, so any
    /// later code in this class that needed the real request would silently get
    /// the action instead. ActionName keeps the route and the view name as
    /// "Request", so the URL and the asp-action links are unaffected.
    /// </remarks>
    [HttpGet]
    [ActionName("Request")]
    public async Task<IActionResult> RequestItem(string? libraryCode)
    {
        var branch = await _branchContext.GetCurrentBranchAsync();

        return View(new TransferRequestViewModel
        {
            LibraryCode = libraryCode ?? string.Empty,
            ToBranchId = branch?.Id,
            Branches = await _branches.GetOpenAsync()
        });
    }

    [HttpPost]
    [ActionName("Request")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestItem(TransferRequestViewModel form)
    {
        if (!ModelState.IsValid)
        {
            form.Branches = await _branches.GetOpenAsync();
            return View(form);
        }

        var result = await _transfers.RequestAsync(form.LibraryCode, form.ToBranchId!.Value, form.Reason);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            form.Branches = await _branches.GetOpenAsync();
            return View(form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dispatch(int id, string? notes)
    {
        var result = await _transfers.DispatchAsync(id, notes);
        SetFlash(result.Succeeded, result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(int id, string? notes)
    {
        var result = await _transfers.ReceiveAsync(id, notes);
        SetFlash(result.Succeeded, result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        var result = await _transfers.CancelAsync(id, reason);
        SetFlash(result.Succeeded, result.Message);
        return RedirectToAction(nameof(Index));
    }

    private void SetFlash(bool succeeded, string message)
    {
        if (succeeded)
        {
            TempData["Success"] = message;
        }
        else
        {
            TempData["Error"] = message;
        }
    }
}
