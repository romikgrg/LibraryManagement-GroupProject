using LibraryManagement.Models;

namespace LibraryManagement.Services;

/// <summary>
/// The holds queue. Patrons join the back of the line for an item that is out or
/// being repaired, and the person at the front is told when it comes back.
/// </summary>
public interface IReservationService
{
    Task<ServiceResult<Reservation>> PlaceAsync(string libraryCode, int borrowerId, int? collectionBranchId = null);
    Task<ServiceResult> CancelAsync(int reservationId, string? reason);

    /// <summary>
    /// Called when an item becomes available again. Promotes the next waiting
    /// member, puts the item on the holds shelf and tells them it is there.
    /// Returns null when nobody is waiting, which is the common case.
    /// </summary>
    Task<Reservation?> PromoteNextAsync(int itemId);

    /// <summary>
    /// Closes holds nobody collected and passes each item to the next in line.
    /// Driven from the staff dashboard, standing in for a nightly job.
    /// </summary>
    Task<int> ExpireLapsedAsync();

    Task<IReadOnlyList<Reservation>> GetQueueAsync(int itemId);
    Task<IReadOnlyList<Reservation>> GetActiveAsync(int? branchId = null);
    Task<IReadOnlyList<Reservation>> GetForBorrowerAsync(int borrowerId);
    Task<Reservation?> GetAsync(int id);

    /// <summary>Where this member sits in the queue for this item, counting from one.</summary>
    Task<int> GetQueuePositionAsync(int reservationId);
}
