using Microsoft.Extensions.Options;

namespace LibraryManagement.Services;


public class DailyRateFinePolicy : IFinePolicy
{
    private readonly LoanRules _rules;

    public DailyRateFinePolicy(IOptions<LoanRules> rules)
    {
        _rules = rules.Value;
    }

    public int LoanPeriodDays => _rules.LoanPeriodDays;

    public string Description =>
        $"Items are due after {_rules.LoanPeriodDays} days. Late returns are charged " +
        $"{_rules.DailyFineRate:C} per day up to a maximum of {_rules.MaximumFine:C}.";

    public decimal CalculateFine(DateTime dueOn, DateTime returnedOn)
    {
        var daysLate = (returnedOn.Date - dueOn.Date).Days - _rules.GraceDays;
        if (daysLate <= 0)
        {
            return 0m;
        }

        var fine = daysLate * _rules.DailyFineRate;
        return Math.Round(Math.Min(fine, _rules.MaximumFine), 2);
    }
}
