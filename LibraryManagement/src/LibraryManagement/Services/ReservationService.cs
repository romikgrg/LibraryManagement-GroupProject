using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LibraryManagement.Services;

/// <summary>
/// The holds queue and the rules that govern it.
/// </summary>
/// <remarks>
/// Queue order comes from <see cref="Reservation.PlacedOn"/> rather than a
/// stored position number. Nothing has to be renumbered when somebody cancels,
/// and there is no way for two rows to claim the same place.
/// </remarks>
public class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _context;
    private readonly IClock _clock;
    private readonly INotificationService _notifications;
    private readonly NotificationRules _rules;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(
        ApplicationDbContext context,
        IClock clock,
        INotificationService notifications,
        IOptions<NotificationRules> rules,
        ILogger<ReservationService> logger)
    {
        _context = context;
        _clock = clock;
        _notifications = notifications;
        _rules = rules.Value;
        _logger = logger;
    }

    public async Task<ServiceResult<Reservation>> PlaceAsync(string libraryCode, int borrowerId,
        int? collectionBranchId = null)
    {
        var code = Normalise(libraryCode);

        var item = await _context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.LibraryCode.ToUpper() == code);
        if (item is null)
        {
            return ServiceResult<Reservation>.Fail($"No item was found with the library code {code}.");
        }

        var borrower = await _context.Borrowers.AsNoTracking().FirstOrDefaultAsync(b => b.Id == borrowerId);
        if (borrower is null)
        {
            return ServiceResult<Reservation>.Fail("That member could not be found.");
        }

        if (!borrower.IsActive)
        {
            return ServiceResult<Reservation>.Fail($"{borrower.FullName}'s membership is not active.");
        }

        if (item.Status == ItemStatus.Available)
        {
            return ServiceResult<Reservation>.Fail(
                $"{item.Name} is on the shelf right now. It can be borrowed rather than reserved.");
        }

        if (item.Status == ItemStatus.Destroyed)
        {
            return ServiceResult<Reservation>.Fail(
                $"{item.Name} has been destroyed and will not return to the collection.");
        }

        if (await _context.Reservations.AnyAsync(r => r.ItemId == item.Id && r.BorrowerId == borrowerId &&
                (r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready)))
        {
            return ServiceResult<Reservation>.Fail($"{borrower.FullName} is already in the queue for {item.Name}.");
        }

        // Somebody holding an item cannot also be waiting for it.
        if (await _context.Loans.AnyAsync(l => l.ItemId == item.Id && l.BorrowerId == borrowerId && l.ReturnedOn == null))
        {
            return ServiceResult<Reservation>.Fail($"{borrower.FullName} already has {item.Name} on loan.");
        }

        var reservation = new Reservation
        {
            ItemId = item.Id,
            BorrowerId = borrowerId,
            CollectionBranchId = collectionBranchId ?? borrower.HomeBranchId,
            PlacedOn = _clock.Now,
            Status = ReservationStatus.Waiting
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        var ahead = await _context.Reservations.CountAsync(r =>
            r.ItemId == item.Id &&
            r.Status == ReservationStatus.Waiting &&
            r.PlacedOn < reservation.PlacedOn);

        _logger.LogInformation("Reservation placed on {Code} for member {BorrowerId}.", item.LibraryCode, borrowerId);

        var position = ahead + 1;
        var message = ahead == 0
            ? $"{borrower.FullName} is first in line for {item.Name}."
            : $"{borrower.FullName} is number {position} in the queue for {item.Name}.";

        return ServiceResult<Reservation>.Ok(reservation, message);
    }

    public async Task<ServiceResult> CancelAsync(int reservationId, string? reason)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Item)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation is null)
        {
            return ServiceResult.Fail("That reservation could not be found.");
        }

        if (!reservation.IsActive)
        {
            return ServiceResult.Fail($"This reservation is already marked as {reservation.Status}.");
        }

        var wasReady = reservation.Status == ReservationStatus.Ready;

        reservation.Status = ReservationStatus.Cancelled;
        reservation.ClosedOn = _clock.Now;
        reservation.Notes = string.IsNullOrWhiteSpace(reason) ? reservation.Notes : reason.Trim();

        await _context.SaveChangesAsync();

        // Cancelling a hold that was already on the shelf frees the item, so the
        // next person in the queue gets their turn immediately.
        if (wasReady)
        {
            await ReleaseAndPromoteAsync(reservation.ItemId);
        }

        _logger.LogInformation("Reservation {Id} cancelled.", reservationId);
        return ServiceResult.Ok($"The hold on {reservation.Item?.Name} has been cancelled.");
    }

    public async Task<Reservation?> PromoteNextAsync(int itemId)
    {
        var next = await _context.Reservations
            .Include(r => r.CollectionBranch)
            .Include(r => r.Borrower)
            .Where(r => r.ItemId == itemId && r.Status == ReservationStatus.Waiting)
            .OrderBy(r => r.PlacedOn)
            .FirstOrDefaultAsync();

        if (next is null)
        {
            return null;
        }

        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        if (item is null)
        {
            return null;
        }

        next.Status = ReservationStatus.Ready;
        next.ReadyOn = _clock.Now;
        next.ExpiresOn = _clock.Today.AddDays(_rules.HoldShelfDays);

        // The item is set aside rather than returned to general stock, so nobody
        // else can walk off with something that is spoken for.
        item.Status = ItemStatus.AwaitingCollection;

        await _context.SaveChangesAsync();

        await _notifications.NotifyAsync(
            NotificationKind.ReservationReady,
            next.BorrowerId,
            itemId,
            new Dictionary<string, string>
            {
                ["branch"] = next.CollectionBranch?.Name ?? "your branch",
                ["expiresOn"] = next.ExpiresOn?.ToString("d MMMM yyyy") ?? ""
            });

        _logger.LogInformation("Reservation {Id} promoted to ready for item {ItemId}.", next.Id, itemId);
        return next;
    }

    public async Task<int> ExpireLapsedAsync()
    {
        var today = _clock.Today;

        var lapsed = await _context.Reservations
            .Include(r => r.Item)
            .Where(r => r.Status == ReservationStatus.Ready && r.ExpiresOn != null && r.ExpiresOn < today)
            .ToListAsync();

        if (lapsed.Count == 0)
        {
            return 0;
        }

        foreach (var reservation in lapsed)
        {
            reservation.Status = ReservationStatus.Expired;
            reservation.ClosedOn = _clock.Now;
        }

        await _context.SaveChangesAsync();

        // Tell each member, then offer the item to whoever is next.
        foreach (var reservation in lapsed)
        {
            await _notifications.NotifyAsync(
                NotificationKind.ReservationExpired, reservation.BorrowerId, reservation.ItemId);

            await ReleaseAndPromoteAsync(reservation.ItemId);
        }

        _logger.LogInformation("Expired {Count} uncollected hold(s).", lapsed.Count);
        return lapsed.Count;
    }

    /// <summary>
    /// Puts an item that was on the holds shelf back into play: either the next
    /// person in the queue claims it, or it returns to general stock.
    /// </summary>
    private async Task ReleaseAndPromoteAsync(int itemId)
    {
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        if (item is null || item.Status != ItemStatus.AwaitingCollection)
        {
            return;
        }

        item.Status = ItemStatus.Available;
        await _context.SaveChangesAsync();

        await PromoteNextAsync(itemId);
    }

    public async Task<IReadOnlyList<Reservation>> GetQueueAsync(int itemId) =>
        await BaseQuery()
            .Where(r => r.ItemId == itemId &&
                        (r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready))
            .OrderBy(r => r.PlacedOn)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IReadOnlyList<Reservation>> GetActiveAsync(int? branchId = null)
    {
        IQueryable<Reservation> query = BaseQuery()
            .Where(r => r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready);

        if (branchId.HasValue)
        {
            query = query.Where(r => r.CollectionBranchId == branchId.Value);
        }

        // Holds already on the shelf come first: those have a clock running.
        return await query
            .OrderByDescending(r => r.Status == ReservationStatus.Ready)
            .ThenBy(r => r.PlacedOn)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Reservation>> GetForBorrowerAsync(int borrowerId) =>
        await BaseQuery()
            .Where(r => r.BorrowerId == borrowerId)
            .OrderByDescending(r => r.PlacedOn)
            .AsNoTracking()
            .ToListAsync();

    public Task<Reservation?> GetAsync(int id) =>
        BaseQuery().AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

    public async Task<int> GetQueuePositionAsync(int reservationId)
    {
        var reservation = await _context.Reservations.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation is null || !reservation.IsActive)
        {
            return 0;
        }

        var ahead = await _context.Reservations.CountAsync(r =>
            r.ItemId == reservation.ItemId &&
            (r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready) &&
            r.PlacedOn < reservation.PlacedOn);

        return ahead + 1;
    }

    private IQueryable<Reservation> BaseQuery() =>
        _context.Reservations
            .Include(r => r.Item)
            .Include(r => r.Borrower)
            .Include(r => r.CollectionBranch);

    private static string Normalise(string code) => (code ?? string.Empty).Trim().ToUpperInvariant();
}
