namespace LibraryManagement.Services;

/// <summary>
/// Works out what a late return costs. The loan service depends on this
/// interface, not on the arithmetic, so a different policy - a flat fee, a
/// student concession, an amnesty week - is a new class and one line in
/// Program.cs.
/// </summary>
public interface IFinePolicy
{
    /// <summary>The standard loan period, used when a new loan is created.</summary>
    int LoanPeriodDays { get; }

    /// <summary>A short description of the policy, shown to staff and members.</summary>
    string Description { get; }

    /// <summary>The fine for an item due on <paramref name="dueOn"/> and handed
    /// back on <paramref name="returnedOn"/>. Zero when it is not late.</summary>
    decimal CalculateFine(DateTime dueOn, DateTime returnedOn);
}
