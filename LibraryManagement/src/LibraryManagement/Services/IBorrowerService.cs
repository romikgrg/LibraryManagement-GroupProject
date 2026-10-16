using LibraryManagement.Models;

namespace LibraryManagement.Services;

/// <summary>Borrower records, maintained by the front desk.</summary>
public interface IBorrowerService
{
    Task<IReadOnlyList<Borrower>> SearchAsync(string? query);
    Task<IReadOnlyList<Borrower>> GetActiveAsync();
    Task<Borrower?> GetAsync(int id);
    Task<Borrower?> GetWithHistoryAsync(int id);

    /// <summary>
    /// Finds a member by the number on their card. This is how the kiosk
    /// identifies somebody, since a patron has their card, not a database id.
    /// </summary>
    Task<Borrower?> GetByMembershipNumberAsync(string membershipNumber);
    Task<ServiceResult<Borrower>> CreateAsync(Borrower borrower);
    Task<ServiceResult> UpdateAsync(Borrower borrower);
    Task<ServiceResult> DeleteAsync(int id);
    Task<string> NextMembershipNumberAsync();
}
