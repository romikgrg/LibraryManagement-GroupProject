using LibraryManagement.Models;
using LibraryManagement.Models.ViewModels;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

/// <summary>
/// The front desk: lending items out, taking them back, and receipting fines.
/// The controller only gathers input and chooses a view. Whether a loan is
/// allowed is decided by <see cref="ILoanService"/>.
/// </summary>
[Authorize(Roles = Roles.Reception)]
public class CirculationController : Controller
{
    private readonly ILoanService _loans;
    private readonly IBorrowerService _borrowers;
    private readonly IFinePolicy _finePolicy;
    private readonly IBranchContext _branchContext;

    public CirculationController(
        ILoanService loans,
        IBorrowerService borrowers,
        IFinePolicy finePolicy,
        IBranchContext branchContext)
    {
        _loans = loans;
        _borrowers = borrowers;
        _finePolicy = finePolicy;
        _branchContext = branchContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Overdue"] = await _loans.GetOverdueLoansAsync();
        ViewData["Recent"] = await _loans.GetRecentActivityAsync();
        ViewData["LendingTerms"] = _finePolicy.Description;
        ViewData["CurrentBranch"] = await _branchContext.GetCurrentBranchAsync();
        return View(await _loans.GetOpenLoansAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Borrow(string? libraryCode)
    {
        return View(new BorrowViewModel
        {
            LibraryCode = libraryCode ?? string.Empty,
            Borrowers = await _borrowers.GetActiveAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrow(BorrowViewModel form)
    {
        if (!ModelState.IsValid)
        {
            form.Borrowers = await _borrowers.GetActiveAsync();
            return View(form);
        }

        // Stamp the loan with the desk taking it, so per-branch figures are right.
        var branchId = await _branchContext.GetOperatingBranchIdAsync();
        var result = await _loans.BorrowAsync(form.LibraryCode, form.BorrowerId!.Value, branchId);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            form.Borrowers = await _borrowers.GetActiveAsync();
            return View(form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Borrow));
    }

    [HttpGet]
    public IActionResult Return(string? libraryCode)
    {
        return View(new ReturnViewModel { LibraryCode = libraryCode ?? string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(ReturnViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        // An item can be returned to any branch, not only the one that lent it.
        var branchId = await _branchContext.GetOperatingBranchIdAsync();
        var result = await _loans.ReturnAsync(form.LibraryCode, form.Condition, form.Notes, branchId);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(form);
        }

        // A fine needs to stay on screen with a "take payment" button, so the
        // completed loan is shown on this page rather than redirected away.
        ModelState.Clear();
        return View(new ReturnViewModel { CompletedLoan = result.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayFine(int loanId, string? returnUrl)
    {
        var result = await _loans.PayFineAsync(loanId);

        if (result.Succeeded)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            // Only ever redirect inside this site, so a tampered query string
            // cannot send staff to somebody else's login page.
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
