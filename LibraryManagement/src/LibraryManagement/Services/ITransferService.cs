using LibraryManagement.Models;

namespace LibraryManagement.Services;

/// <summary>
/// Moving stock between branches. A transfer is a three step handshake:
/// the destination requests, the holder dispatches, the destination receives.
/// </summary>
public interface ITransferService
{
    Task<ServiceResult<Transfer>> RequestAsync(string libraryCode, int toBranchId, string? reason, int? reservationId = null);
    Task<ServiceResult<Transfer>> DispatchAsync(int transferId, string? notes);
    Task<ServiceResult<Transfer>> ReceiveAsync(int transferId, string? notes);
    Task<ServiceResult> CancelAsync(int transferId, string? reason);

    Task<IReadOnlyList<Transfer>> GetOpenAsync(int? branchId = null);
    Task<IReadOnlyList<Transfer>> GetIncomingAsync(int branchId);
    Task<IReadOnlyList<Transfer>> GetOutgoingAsync(int branchId);
    Task<IReadOnlyList<Transfer>> GetRecentAsync(int count = 20);
    Task<Transfer?> GetAsync(int id);
}
