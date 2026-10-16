using LibraryManagement.Models;

namespace LibraryManagement.Services;

/// <summary>Everything the application does with items, in one contract.</summary>
public interface IItemService
{
    /// <param name="branchId">Restrict to one branch's shelves, or null for the whole network.</param>
    Task<IReadOnlyList<Item>> SearchAsync(string? query, string? type, ItemStatus? status,
        bool availableOnly = false, int? branchId = null);
    Task<Item?> GetAsync(int id);
    Task<Item?> GetByCodeAsync(string libraryCode);
    Task<Item?> GetWithHistoryAsync(int id);
    Task<ServiceResult<Item>> CreateAsync(Item item);
    Task<ServiceResult> UpdateAsync(int id, Action<Item> applyChanges, string newLibraryCode);
    Task<ServiceResult> DeleteAsync(int id);
    Task<bool> LibraryCodeExistsAsync(string libraryCode, int? excludeId = null);
}
