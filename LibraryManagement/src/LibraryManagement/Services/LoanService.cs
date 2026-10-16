using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Models.ViewModels;
using LibraryManagement.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LibraryManagement.Services;

/// <summary>
/// The lending rules of the library. This class owns the decision about whether
/// an item may go out, how long it may stay out and what it costs when it comes
/// back late. It asks <see cref="IFinePolicy"/> for the money,
/// <see cref="IClock"/> for the date, <see cref="IReservationService"/> whether
/// anybody is waiting and <see cref="INotificationService"/> to tell the member,
/// so none of those four decisions is hard coded here.
/// </summary>
public class LoanService : ILoanService
{
    private readonly ApplicationDbContext _context;
    private readonly IFinePolicy _finePolicy;
    private readonly IClock _clock;
    private readonly LoanRules _rules;
    private readonly IReservationService _reservations;
    private readonly INotificationService _notifications;
    private readonly ILogger<LoanService> _logger;

    public LoanService(
        ApplicationDbContext context,
        IFinePolicy finePolicy,
        IClock clock,
        IOptions<LoanRules> rules,
        IReservationService reservations,
        INotificationService notifications,
        ILogger<LoanService> logger)
    {
        _context = context;
        _finePolicy = finePolicy;
        _clock = clock;
        _rules = rules.Value;
        _reservations = reservations;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<ServiceResult<Loan>> BorrowAsync(string libraryCode, int borrowerId, int? branchId = null)
    {
        var code = Normalise(libraryCode);

        var item = await _context.Items.FirstOrDefaultAsync(i => i.LibraryCode.ToUpper() == code);
        if (item is null)
        {
            return ServiceResult<Loan>.Fail($"No item was found with the library code {code}.");
        }

        // An item on the holds shelf is reserved for one person. Only they can
        // take it, and doing so closes their reservation.
        Reservation? claimedHold = null;
        if (item.Status == ItemStatus.AwaitingCollection)
        {
            claimedHold = await _context.Reservations
                .Where(r => r.ItemId == item.Id && r.Status == ReservationStatus.Ready)
                .OrderBy(r => r.PlacedOn)
                .FirstOrDefaultAsync();

            if (claimedHold is null)
            {
                // The shelf status is stale: no live hold explains it, so release it.
                item.Status = ItemStatus.Available;
            }
            else if (claimedHold.BorrowerId != borrowerId)
            {
                return ServiceResult<Loan>.Fail(
                    $"{item.Name} is on the holds shelf for another member and cannot be lent out.");
            }
        }

        if (item.Status != ItemStatus.Available && claimedHold is null)
        {
            return ServiceResult<Loan>.Fail($"{item.Name} cannot be borrowed because it is marked as {item.Status}.");
        }

        // Belt and braces: the status column says it is free, but the loan table is
        // the record that matters.
        if (await _context.Loans.AnyAsync(l => l.ItemId == item.Id && l.ReturnedOn == null))
        {
            return ServiceResult<Loan>.Fail($"{item.Name} is already out on loan.");
        }

        var borrower = await _context.Borrowers.FirstOrDefaultAsync(b => b.Id == borrowerId);
        if (borrower is null)
        {
            return ServiceResult<Loan>.Fail("That member could not be found.");
        }

        if (!borrower.IsActive)
        {
            return ServiceResult<Loan>.Fail($"{borrower.FullName}'s membership is not active.");
        }

        var openLoans = await _context.Loans.CountAsync(l => l.BorrowerId == borrowerId && l.ReturnedOn == null);
        if (openLoans >= _rules.MaximumOpenLoans)
        {
            return ServiceResult<Loan>.Fail(
                $"{borrower.FullName} already has {openLoans} items out, which is the limit of {_rules.MaximumOpenLoans}.");
        }

        // SQLite stores decimals as text, so the outstanding total is added up in
        // memory rather than in SQL.
        var unpaid = await _context.Loans
            .Where(l => l.BorrowerId == borrowerId && !l.FinePaid && l.FineAmount > 0)
            .Select(l => l.FineAmount)
            .ToListAsync();

        var owing = unpaid.Sum();
        if (owing >= _rules.FineLimitBeforeBlock)
        {
            return ServiceResult<Loan>.Fail(
                $"{borrower.FullName} owes {owing:C} in fines. Borrowing resumes once the balance is under {_rules.FineLimitBeforeBlock:C}.");
        }

        var today = _clock.Today;
        var loan = new Loan
        {
            ItemId = item.Id,
            BorrowerId = borrower.Id,
            // The desk doing the lending, falling back to wherever the item is.
            BranchId = branchId ?? item.BranchId,
            BorrowedOn = today,
            DueOn = today.AddDays(_finePolicy.LoanPeriodDays)
        };

        item.Status = ItemStatus.Borrowed;

        // Taking a held item off the shelf closes the hold that reserved it.
        if (claimedHold is not null)
        {
            claimedHold.Status = ReservationStatus.Collected;
            claimedHold.ClosedOn = _clock.Now;
        }

        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        loan.Item = item;
        loan.Borrower = borrower;

        await _notifications.NotifyAsync(
            NotificationKind.Borrowed, borrower.Id, item.Id,
            new Dictionary<string, string> { ["dueOn"] = loan.DueOn.ToString("d MMMM yyyy") });

        _logger.LogInformation("{Code} borrowed by {Membership}, due {Due:d}.",
            item.LibraryCode, borrower.MembershipNumber, loan.DueOn);

        var message = $"{item.Name} is on loan to {borrower.FullName} and is due back on {loan.DueOn:d MMMM yyyy}.";
        if (claimedHold is not null)
        {
            message += " Their hold has been closed as collected.";
        }

        return ServiceResult<Loan>.Ok(loan, message);
    }

    public async Task<ServiceResult<Loan>> ReturnAsync(string libraryCode, ReturnCondition condition,
        string? notes, int? branchId = null)
    {
        var code = Normalise(libraryCode);

        var loan = await _context.Loans
            .Include(l => l.Item)
            .Include(l => l.Borrower)
            .FirstOrDefaultAsync(l => l.ReturnedOn == null && l.Item!.LibraryCode.ToUpper() == code);

        if (loan is null)
        {
            var exists = await _context.Items.AnyAsync(i => i.LibraryCode.ToUpper() == code);
            return ServiceResult<Loan>.Fail(exists
                ? $"The item with code {code} is not currently on loan."
                : $"No item was found with the library code {code}.");
        }

        var today = _clock.Today;
        loan.ReturnedOn = today;
        loan.ReturnBranchId = branchId ?? loan.BranchId;
        loan.FineAmount = _finePolicy.CalculateFine(loan.DueOn, today);
        loan.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        var item = loan.Item!;

        item.Status = condition switch
        {
            ReturnCondition.Damaged => ItemStatus.Damaged,
            ReturnCondition.Destroyed => ItemStatus.Destroyed,
            _ => ItemStatus.Available
        };

        // Returning somewhere other than where it went out is how stock drifts
        // around the network. The item now lives at the branch that took it back.
        if (branchId.HasValue && item.BranchId != branchId.Value)
        {
            item.BranchId = branchId.Value;
        }

        await _context.SaveChangesAsync();

        var daysLate = loan.DaysOverdue(today);
        var message = daysLate > 0
            ? $"{item.Name} returned {daysLate} day{(daysLate == 1 ? "" : "s")} late. Fine due: {loan.FineAmount:C}."
            : $"{item.Name} returned on time. No fine.";

        if (condition != ReturnCondition.Good)
        {
            message += $" The item has been marked as {item.Status}.";
        }

        if (loan.FineAmount > 0)
        {
            await _notifications.NotifyAsync(
                NotificationKind.FineCharged, loan.BorrowerId, item.Id,
                new Dictionary<string, string> { ["amount"] = loan.FineAmount.ToString("C") });
        }

        // An item back on the shelf may already be spoken for. Promoting the next
        // person in the queue moves it to the holds shelf and tells them it is in.
        if (item.Status == ItemStatus.Available)
        {
            var promoted = await _reservations.PromoteNextAsync(item.Id);
            if (promoted is not null)
            {
                message += $" It is reserved, so it has gone onto the holds shelf for {promoted.Borrower?.FullName ?? "the next member in the queue"}.";
            }
        }

        _logger.LogInformation("{Code} returned. Days late: {Days}. Fine: {Fine}.",
            item.LibraryCode, daysLate, loan.FineAmount);

        return ServiceResult<Loan>.Ok(loan, message);
    }

    public async Task<ServiceResult> PayFineAsync(int loanId)
    {
        var loan = await _context.Loans.Include(l => l.Item).FirstOrDefaultAsync(l => l.Id == loanId);
        if (loan is null)
        {
            return ServiceResult.Fail("That loan could not be found.");
        }

        if (loan.FineAmount <= 0)
        {
            return ServiceResult.Fail("There is no fine on that loan.");
        }

        if (loan.FinePaid)
        {
            return ServiceResult.Fail("That fine has already been paid.");
        }

        loan.FinePaid = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Fine of {Fine} paid on loan {LoanId}.", loan.FineAmount, loan.Id);
        return ServiceResult.Ok($"Receipted {loan.FineAmount:C} for {loan.Item?.Name}.");
    }

    public async Task<IReadOnlyList<Loan>> GetOpenLoansAsync() =>
        await _context.Loans
            .Include(l => l.Item)
            .Include(l => l.Borrower)
            .Include(l => l.Branch)
            .Where(l => l.ReturnedOn == null)
            .OrderBy(l => l.DueOn)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IReadOnlyList<Loan>> GetOverdueLoansAsync()
    {
        var today = _clock.Today;
        return await _context.Loans
            .Include(l => l.Item)
            .Include(l => l.Borrower)
            .Include(l => l.Branch)
            .Where(l => l.ReturnedOn == null && l.DueOn < today)
            .OrderBy(l => l.DueOn)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Loan>> GetRecentActivityAsync(int count = 10) =>
        await _context.Loans
            .Include(l => l.Item)
            .Include(l => l.Borrower)
            .Include(l => l.Branch)
            .OrderByDescending(l => l.ReturnedOn ?? l.BorrowedOn)
            .ThenByDescending(l => l.Id)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();

    public Task<Loan?> GetAsync(int id) =>
        _context.Loans
            .Include(l => l.Item)
            .Include(l => l.Borrower)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);

    private static string Normalise(string code) => (code ?? string.Empty).Trim().ToUpperInvariant();
}
