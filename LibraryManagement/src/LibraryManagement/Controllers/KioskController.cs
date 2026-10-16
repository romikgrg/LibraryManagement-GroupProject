using LibraryManagement.Models;
using LibraryManagement.Models.ViewModels;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

/// <summary>
/// The self-service terminal that stands in the foyer.
/// </summary>
/// <remarks>
/// A separate controller rather than a skin over the reception screens, because
/// the audience is different in a way that changes every decision. A patron at a
/// kiosk has no account, no training and no mouse; they have a card, an item and
/// about thirty seconds. So this controller does one thing per request, never
/// asks for anything it can look up, and answers in plain words rather than
/// staff terminology. It also exposes no borrower data beyond the card that was
/// presented, which is why none of the reception actions are reachable from here.
/// </remarks>
[AllowAnonymous]
public class KioskController : Controller
{
    private readonly IItemService _items;
    private readonly IBorrowerService _borrowers;
    private readonly ILoanService _loans;
    private readonly IReservationService _reservations;
    private readonly IBranchContext _branchContext;
    private readonly IClock _clock;

    public KioskController(
        IItemService items,
        IBorrowerService borrowers,
        ILoanService loans,
        IReservationService reservations,
        IBranchContext branchContext,
        IClock clock)
    {
        _items = items;
        _borrowers = borrowers;
        _loans = loans;
        _reservations = reservations;
        _branchContext = branchContext;
        _clock = clock;
    }

    /// <summary>The idle screen: two big buttons.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(new KioskViewModel { Branch = await CurrentBranchAsync() });
    }

    /// <summary>Step one of borrowing: identify the patron.</summary>
    [HttpGet]
    public async Task<IActionResult> Borrow()
    {
        return View(new KioskViewModel { Branch = await CurrentBranchAsync() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrow(string? membershipNumber, string? libraryCode)
    {
        var model = new KioskViewModel
        {
            Branch = await CurrentBranchAsync(),
            MembershipNumber = membershipNumber,
            LibraryCode = libraryCode
        };

        if (string.IsNullOrWhiteSpace(membershipNumber))
        {
            model.Problem = "Please scan your library card first.";
            return View(model);
        }

        var member = await _borrowers.GetByMembershipNumberAsync(membershipNumber);
        if (member is null)
        {
            model.Problem = "That card was not recognised. Please see a staff member at the desk.";
            return View(model);
        }

        model.Member = member;

        // Card accepted but nothing scanned yet: show the second step rather than
        // treating an empty box as an error.
        if (string.IsNullOrWhiteSpace(libraryCode))
        {
            return View(model);
        }

        var branchId = await _branchContext.GetOperatingBranchIdAsync();
        var result = await _loans.BorrowAsync(libraryCode, member.Id, branchId);

        if (result.Succeeded && result.Value is not null)
        {
            model.Success = $"{result.Value.Item?.Name} is yours until {result.Value.DueOn:dddd d MMMM}.";
            model.ScannedItem = result.Value.Item;
            model.LibraryCode = null; // Ready for the next item.
        }
        else
        {
            // The service's messages are written for staff. The kiosk softens the
            // ones a patron would find alarming and sends them to the desk.
            model.Problem = Soften(result.Message);
        }

        return View(model);
    }

    /// <summary>Account summary: what the patron has out, what is waiting, what is owed.</summary>
    [HttpGet]
    public async Task<IActionResult> Account()
    {
        return View(new KioskViewModel { Branch = await CurrentBranchAsync() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Account(string? membershipNumber)
    {
        var model = new KioskViewModel
        {
            Branch = await CurrentBranchAsync(),
            MembershipNumber = membershipNumber
        };

        if (string.IsNullOrWhiteSpace(membershipNumber))
        {
            model.Problem = "Please scan your library card.";
            return View(model);
        }

        var member = await _borrowers.GetWithHistoryAsync(
            (await _borrowers.GetByMembershipNumberAsync(membershipNumber))?.Id ?? 0);

        if (member is null)
        {
            model.Problem = "That card was not recognised. Please see a staff member at the desk.";
            return View(model);
        }

        var today = _clock.Today;
        var open = member.Loans.Where(l => l.ReturnedOn == null).OrderBy(l => l.DueOn).ToList();

        model.Member = member;
        model.MemberLoans = open;
        model.OverdueCount = open.Count(l => l.DueOn.Date < today);
        model.OutstandingFines = member.Loans.Where(l => !l.FinePaid).Sum(l => l.FineAmount);
        model.MemberHolds = (await _reservations.GetForBorrowerAsync(member.Id))
            .Where(r => r.IsActive)
            .ToList();

        return View(model);
    }

    /// <summary>Look up one item without signing in, for "where is this?".</summary>
    [HttpGet]
    public async Task<IActionResult> Find(string? libraryCode)
    {
        var model = new KioskViewModel
        {
            Branch = await CurrentBranchAsync(),
            LibraryCode = libraryCode
        };

        if (!string.IsNullOrWhiteSpace(libraryCode))
        {
            model.ScannedItem = await _items.GetByCodeAsync(libraryCode);
            if (model.ScannedItem is null)
            {
                model.Problem = "No item was found with that code. Please check and try again.";
            }
        }

        return View(model);
    }

    private Task<Branch?> CurrentBranchAsync() => _branchContext.GetDeploymentBranchAsync();

    /// <summary>
    /// Rewrites the handful of staff-facing refusals that would worry or confuse
    /// somebody standing at a machine on their own.
    /// </summary>
    private static string Soften(string message)
    {
        if (message.Contains("fines", StringComparison.OrdinalIgnoreCase))
        {
            return "There is something on your account that needs sorting out. Please see a staff member at the desk.";
        }

        if (message.Contains("not active", StringComparison.OrdinalIgnoreCase))
        {
            return "Your membership needs renewing. Please see a staff member at the desk.";
        }

        if (message.Contains("holds shelf", StringComparison.OrdinalIgnoreCase))
        {
            return "This item is being held for another member. Please see a staff member at the desk.";
        }

        return message;
    }
}
