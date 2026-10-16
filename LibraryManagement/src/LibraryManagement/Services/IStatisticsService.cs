using LibraryManagement.Models.ViewModels;

namespace LibraryManagement.Services;

/// <summary>Builds the manager's reports.</summary>
public interface IStatisticsService
{
    /// <param name="branchId">One branch's figures, or null for the whole network.</param>
    Task<DashboardViewModel> BuildDashboardAsync(int? branchId = null);
}
