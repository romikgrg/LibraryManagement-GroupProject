namespace LibraryManagement.Models.ViewModels;

/// <summary>Everything on the manager's reporting screen, in one object.</summary>
public class DashboardViewModel
{
    public DateTime GeneratedOn { get; set; }

    /// <summary>The branch these figures cover, or null for the whole network.</summary>
    public int? BranchId { get; set; }

    public BorrowingStatistics Borrowing { get; set; } = new();
    public ItemStatistics Items { get; set; } = new();
    public FineStatistics Fines { get; set; } = new();

    /// <summary>Transfers and holds: the figures that only exist across branches.</summary>
    public NetworkStatistics Network { get; set; } = new();

    public bool IsNetworkWide => BranchId is null;
}

/// <summary>How stock and holds are moving around the branch network.</summary>
public class NetworkStatistics
{
    public int OpenTransfers { get; set; }
    public int InTransit { get; set; }
    public int CompletedTransfers { get; set; }
    public double AverageTransferDays { get; set; }

    public int WaitingHolds { get; set; }
    public int ReadyHolds { get; set; }
    public int LapsedHolds { get; set; }
    public int CollectedHolds { get; set; }

    public IReadOnlyList<CountByLabel> LoansByBranch { get; set; } = Array.Empty<CountByLabel>();
}

public class BorrowingStatistics
{
    public int TotalLoans { get; set; }
    public int LoansThisMonth { get; set; }
    public int CurrentlyOnLoan { get; set; }
    public int OverdueNow { get; set; }
    public double AverageDaysKept { get; set; }
    public IReadOnlyList<CountByLabel> LoansByItemType { get; set; } = Array.Empty<CountByLabel>();
    public IReadOnlyList<CountByLabel> MostBorrowedItems { get; set; } = Array.Empty<CountByLabel>();
    public IReadOnlyList<CountByLabel> BusiestBorrowers { get; set; } = Array.Empty<CountByLabel>();
    public IReadOnlyList<CountByLabel> LoansByMonth { get; set; } = Array.Empty<CountByLabel>();
}

public class ItemStatistics
{
    public int TotalItems { get; set; }
    public IReadOnlyList<CountByLabel> ByStatus { get; set; } = Array.Empty<CountByLabel>();
    public IReadOnlyList<CountByLabel> ByType { get; set; } = Array.Empty<CountByLabel>();
    public IReadOnlyList<Item> OutOfAction { get; set; } = Array.Empty<Item>();
}

public class FineStatistics
{
    public decimal TotalCharged { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Outstanding { get; set; }
    public int LateReturns { get; set; }
    public decimal AverageFine { get; set; }
    public decimal LargestFine { get; set; }
    public IReadOnlyList<Loan> UnpaidFines { get; set; } = Array.Empty<Loan>();
}

/// <summary>A label and a number - the shape every chart and table on the
/// dashboard needs, so the statistics service returns nothing more exotic.</summary>
public class CountByLabel
{
    public CountByLabel() { }

    public CountByLabel(string label, int count)
    {
        Label = label;
        Count = count;
    }

    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
