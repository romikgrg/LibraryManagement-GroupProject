using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Services;

/// <summary>
/// Turns the loan and item tables into the three sets of figures the manager
/// asked for: borrowing, item status and fines.
/// </summary>
/// <remarks>
/// The loans and items are read once and then aggregated in memory. At the size
/// of a branch library that is a single pair of queries and it keeps the money
/// arithmetic in C# decimals rather than in SQLite, which has no decimal type.
/// For a collection large enough to make that expensive, the grouping would move
/// into the database and this class would be the only one that changes.
/// </remarks>
public class StatisticsService : IStatisticsService
{
    private readonly ApplicationDbContext _context;
    private readonly IClock _clock;

    public StatisticsService(ApplicationDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<DashboardViewModel> BuildDashboardAsync(int? branchId = null)
    {
        var today = _clock.Today;

        // Loans are filtered by where the lending happened; items by where they
        // are held now. Those are different questions and a single filter would
        // answer neither correctly.
        var loans = await _context.Loans
            .Include(l => l.Item)
            .Include(l => l.Borrower)
            .Where(l => branchId == null || l.BranchId == branchId)
            .AsNoTracking()
            .ToListAsync();

        var items = await _context.Items
            .Include(i => i.Branch)
            .Where(i => branchId == null || i.BranchId == branchId)
            .AsNoTracking()
            .ToListAsync();

        return new DashboardViewModel
        {
            GeneratedOn = _clock.Now,
            BranchId = branchId,
            Borrowing = BuildBorrowing(loans, today),
            Items = BuildItems(items),
            Fines = BuildFines(loans),
            Network = await BuildNetworkAsync(branchId, today)
        };
    }

    /// <summary>
    /// The figures that only exist once there is more than one branch: stock on
    /// the road, holds waiting, and how the loans split across the network.
    /// </summary>
    private async Task<NetworkStatistics> BuildNetworkAsync(int? branchId, DateTime today)
    {
        var transfers = await _context.Transfers
            .Include(t => t.Item)
            .Include(t => t.FromBranch)
            .Include(t => t.ToBranch)
            .Where(t => branchId == null || t.FromBranchId == branchId || t.ToBranchId == branchId)
            .AsNoTracking()
            .ToListAsync();

        var reservations = await _context.Reservations
            .AsNoTracking()
            .Where(r => branchId == null || r.CollectionBranchId == branchId)
            .Select(r => new { r.Status, r.ExpiresOn })
            .ToListAsync();

        // No Include here: the grouping already joins Branch, and an Include is
        // discarded once a query projects, which EF Core logs a warning about.
        var loansByBranch = await _context.Loans
            .AsNoTracking()
            .GroupBy(l => l.Branch!.Name)
            .Select(g => new { Branch = g.Key, Count = g.Count() })
            .ToListAsync();

        return new NetworkStatistics
        {
            OpenTransfers = transfers.Count(t => t.IsOpen),
            InTransit = transfers.Count(t => t.Status == TransferStatus.InTransit),
            CompletedTransfers = transfers.Count(t => t.Status == TransferStatus.Received),
            AverageTransferDays = transfers.Count(t => t.Status == TransferStatus.Received) == 0
                ? 0
                : Math.Round(transfers
                    .Where(t => t.Status == TransferStatus.Received)
                    .Average(t => t.DaysInProgress(today)), 1),

            WaitingHolds = reservations.Count(r => r.Status == ReservationStatus.Waiting),
            ReadyHolds = reservations.Count(r => r.Status == ReservationStatus.Ready),
            LapsedHolds = reservations.Count(r =>
                r.Status == ReservationStatus.Ready && r.ExpiresOn != null && r.ExpiresOn < today),
            CollectedHolds = reservations.Count(r => r.Status == ReservationStatus.Collected),

            LoansByBranch = loansByBranch
                .Select(b => new CountByLabel(b.Branch ?? "Unknown", b.Count))
                .OrderByDescending(c => c.Count)
                .ToList()
        };
    }

    private static BorrowingStatistics BuildBorrowing(List<Loan> loans, DateTime today)
    {
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var closed = loans.Where(l => l.ReturnedOn.HasValue).ToList();

        return new BorrowingStatistics
        {
            TotalLoans = loans.Count,
            LoansThisMonth = loans.Count(l => l.BorrowedOn >= startOfMonth),
            CurrentlyOnLoan = loans.Count(l => l.IsOpen),
            OverdueNow = loans.Count(l => l.IsOpen && l.DueOn < today),
            AverageDaysKept = closed.Count == 0
                ? 0
                : Math.Round(closed.Average(l => (l.ReturnedOn!.Value - l.BorrowedOn).TotalDays), 1),

            LoansByItemType = loans
                .Where(l => l.Item is not null)
                .GroupBy(l => l.Item!.ItemType)
                .Select(g => new CountByLabel(g.Key, g.Count()))
                .OrderByDescending(c => c.Count)
                .ToList(),

            MostBorrowedItems = loans
                .Where(l => l.Item is not null)
                .GroupBy(l => $"{l.Item!.Name} ({l.Item.LibraryCode})")
                .Select(g => new CountByLabel(g.Key, g.Count()))
                .OrderByDescending(c => c.Count)
                .ThenBy(c => c.Label)
                .Take(5)
                .ToList(),

            BusiestBorrowers = loans
                .Where(l => l.Borrower is not null)
                .GroupBy(l => $"{l.Borrower!.FullName} ({l.Borrower.MembershipNumber})")
                .Select(g => new CountByLabel(g.Key, g.Count()))
                .OrderByDescending(c => c.Count)
                .ThenBy(c => c.Label)
                .Take(5)
                .ToList(),

            LoansByMonth = BuildLastSixMonths(loans, today)
        };
    }

    private static List<CountByLabel> BuildLastSixMonths(List<Loan> loans, DateTime today)
    {
        var months = new List<CountByLabel>();
        for (var offset = 5; offset >= 0; offset--)
        {
            var month = new DateTime(today.Year, today.Month, 1).AddMonths(-offset);
            var next = month.AddMonths(1);
            var count = loans.Count(l => l.BorrowedOn >= month && l.BorrowedOn < next);
            months.Add(new CountByLabel(month.ToString("MMM yyyy"), count));
        }
        return months;
    }

    private static ItemStatistics BuildItems(List<Item> items) => new()
    {
        TotalItems = items.Count,

        ByStatus = Enum.GetValues<ItemStatus>()
            .Select(status => new CountByLabel(status.ToString(), items.Count(i => i.Status == status)))
            .ToList(),

        ByType = items
            .GroupBy(i => i.ItemType)
            .Select(g => new CountByLabel(g.Key, g.Count()))
            .OrderByDescending(c => c.Count)
            .ToList(),

        OutOfAction = items
            .Where(i => i.Status is ItemStatus.Damaged or ItemStatus.Destroyed)
            .OrderBy(i => i.Name)
            .ToList()
    };

    private static FineStatistics BuildFines(List<Loan> loans)
    {
        var fined = loans.Where(l => l.FineAmount > 0).ToList();

        return new FineStatistics
        {
            TotalCharged = fined.Sum(l => l.FineAmount),
            TotalPaid = fined.Where(l => l.FinePaid).Sum(l => l.FineAmount),
            Outstanding = fined.Where(l => !l.FinePaid).Sum(l => l.FineAmount),
            LateReturns = fined.Count,
            AverageFine = fined.Count == 0 ? 0m : Math.Round(fined.Average(l => l.FineAmount), 2),
            LargestFine = fined.Count == 0 ? 0m : fined.Max(l => l.FineAmount),
            UnpaidFines = fined
                .Where(l => !l.FinePaid)
                .OrderByDescending(l => l.FineAmount)
                .ToList()
        };
    }
}
