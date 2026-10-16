using LibraryManagement.Models;
using LibraryManagement.Models.ViewModels;

namespace LibraryManagement.Services;

/// <summary>Borrowing, returning and fines: the front desk's work.</summary>
public interface ILoanService
{
    /// <param name="branchId">The desk doing the lending. Defaults to wherever the item is held.</param>
    Task<ServiceResult<Loan>> BorrowAsync(string libraryCode, int borrowerId, int? branchId = null);

    /// <param name="branchId">The desk taking the return, which may differ from where it went out.</param>
    Task<ServiceResult<Loan>> ReturnAsync(string libraryCode, ReturnCondition condition, string? notes, int? branchId = null);
    Task<ServiceResult> PayFineAsync(int loanId);
    Task<IReadOnlyList<Loan>> GetOpenLoansAsync();
    Task<IReadOnlyList<Loan>> GetOverdueLoansAsync();
    Task<IReadOnlyList<Loan>> GetRecentActivityAsync(int count = 10);
    Task<Loan?> GetAsync(int id);
}
