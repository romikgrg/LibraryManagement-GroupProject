using LibraryManagement.Models;

namespace LibraryManagement.Services;

/// <summary>Branch records and the network-wide lookups other services need.</summary>
public interface IBranchService
{
    Task<IReadOnlyList<Branch>> GetAllAsync();
    Task<IReadOnlyList<Branch>> GetOpenAsync();
    Task<Branch?> GetAsync(int id);
    Task<Branch?> GetByCodeAsync(string code);
    Task<ServiceResult<Branch>> CreateAsync(Branch branch);
    Task<ServiceResult> UpdateAsync(Branch branch);
    Task<ServiceResult> DeleteAsync(int id);

    /// <summary>
    /// The stock position at one branch: how much it holds, how much is out and
    /// how much of what it owns is sitting somewhere else. Feeds both the branch
    /// detail screen and the manager's inventory health report.
    /// </summary>
    Task<BranchInventory> GetInventoryAsync(int branchId);
}

/// <summary>A branch's stock position at a point in time.</summary>
public class BranchInventory
{
    public Branch? Branch { get; set; }
    public int HeldHere { get; set; }
    public int Available { get; set; }
    public int OnLoan { get; set; }
    public int InTransit { get; set; }
    public int AwaitingCollection { get; set; }
    public int OutOfAction { get; set; }

    /// <summary>Owned by this branch but currently sitting at another one.</summary>
    public int OwnedButAway { get; set; }

    /// <summary>Held here but belonging to another branch.</summary>
    public int VisitingStock { get; set; }

    public int OpenReservations { get; set; }

    /// <summary>
    /// Share of the stock held here that a patron could walk out with today.
    /// The headline number on the inventory health report.
    /// </summary>
    public double AvailabilityRate => HeldHere == 0 ? 0 : Math.Round(Available * 100.0 / HeldHere, 1);
}
