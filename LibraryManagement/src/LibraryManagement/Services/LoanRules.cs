namespace LibraryManagement.Services;

/// <summary>
/// The library's lending policy, bound from the "LoanRules" section of
/// appsettings.json. Changing the fine rate is a configuration change, not a
/// code change.
/// </summary>
public class LoanRules
{
    public const string SectionName = "LoanRules";

    /// <summary>How long an item can be kept before it is overdue.</summary>
    public int LoanPeriodDays { get; set; } = 14;

    /// <summary>Charged for each day past the due date.</summary>
    public decimal DailyFineRate { get; set; } = 0.50m;

    /// <summary>A fine never grows past this, no matter how late the item is.</summary>
    public decimal MaximumFine { get; set; } = 20.00m;

    /// <summary>Days of lateness forgiven before any fine starts.</summary>
    public int GraceDays { get; set; } = 0;

    /// <summary>How many items one member may hold at once.</summary>
    public int MaximumOpenLoans { get; set; } = 5;

    /// <summary>Borrowing is blocked once unpaid fines reach this amount.</summary>
    public decimal FineLimitBeforeBlock { get; set; } = 10.00m;
}
