using LibraryManagement.Models;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

/// <summary>Member records, maintained by the front desk.</summary>
[Authorize(Roles = Roles.Reception)]
public class BorrowersController : Controller
{
    private readonly IBorrowerService _borrowers;
    private readonly IBranchService _branches;
    private readonly IBranchContext _branchContext;

    public BorrowersController(IBorrowerService borrowers, IBranchService branches, IBranchContext branchContext)
    {
        _borrowers = borrowers;
        _branches = branches;
        _branchContext = branchContext;
    }

    /// <summary>The home-branch picker is on both the create and the edit form.</summary>
    private async Task PopulateBranchesAsync()
    {
        ViewData["Branches"] = await _branches.GetOpenAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? query)
    {
        ViewData["Query"] = query;
        return View(await _borrowers.SearchAsync(query));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var borrower = await _borrowers.GetWithHistoryAsync(id);
        return borrower is null ? NotFound() : View(borrower);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateBranchesAsync();

        return View(new Borrower
        {
            MembershipNumber = await _borrowers.NextMembershipNumberAsync(),
            JoinedOn = DateTime.Today,
            // New members usually join at the desk they are standing in front of.
            HomeBranchId = await _branchContext.GetOperatingBranchIdAsync() ?? 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Borrower borrower)
    {
        if (!ModelState.IsValid)
        {
            await PopulateBranchesAsync();
            return View(borrower);
        }

        var result = await _borrowers.CreateAsync(borrower);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(borrower.MembershipNumber), result.Message);
            await PopulateBranchesAsync();
            return View(borrower);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var borrower = await _borrowers.GetAsync(id);
        if (borrower is null)
        {
            return NotFound();
        }

        await PopulateBranchesAsync();
        return View(borrower);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Borrower borrower)
    {
        if (id != borrower.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateBranchesAsync();
            return View(borrower);
        }

        var result = await _borrowers.UpdateAsync(borrower);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateBranchesAsync();
            return View(borrower);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var borrower = await _borrowers.GetWithHistoryAsync(id);
        return borrower is null ? NotFound() : View(borrower);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _borrowers.DeleteAsync(id);

        if (result.Succeeded)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
